namespace DG.Daxif.Common

open System
open System.Collections.Generic
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Query
open DG.Daxif
open DG.Daxif.Common
open CrmDataHelper

// http://msdn.microsoft.com/en-us/library/microsoft.xrm.sdk.organizationrequest.requestname.aspx
module CrmDataInternal = 
  
  module internal Info = 

    let private versionHelper v = 
      match v |> Seq.head with
      | '5' -> CrmReleases.CRM2011
      | '6' -> CrmReleases.CRM2013
      | '7' -> CrmReleases.CRM2015
      | '8' -> CrmReleases.CRM2016
      | x when x < '5' -> failwith "Version not supported."
      | _ -> CrmReleases.D365
    
    let version (proxy : OrganizationServiceProxy) = 
      let req = Messages.RetrieveVersionRequest()
      let resp = proxy.Execute(req) :?> Messages.RetrieveVersionResponse
      resp.Version, resp.Version |> versionHelper

    let retrieveAsyncJobState proxy asyncJobId =
      let systemJob = CrmDataHelper.retrieve proxy "asyncoperation" asyncJobId (RetrieveSelect.Fields ["statuscode"])
      systemJob.GetAttributeValue<OptionSetValue>("statuscode")
      |> fun o -> Utility.stringToEnum<AsyncJobState> (o.Value.ToString())

  module internal CRUD =
    let performAsBulkWithOutput proxy (log:ConsoleLogger) reqs =
      let resp = CrmDataHelper.performAsBulk proxy reqs
      resp
      |> Array.fold (fun (count, errs) r -> 
        if r.Fault <> null then
          (count, sprintf "Error when performing %s: %s" (Seq.item r.RequestIndex reqs).RequestName r.Fault.Message :: errs)
        else
          (count + 1, errs)
        ) (0, [])
      |> fun (count, errs) ->
        log.Verbose "Succesfully performed %d/%d actions in %A" count (Seq.length reqs) 
          proxy.ServiceConfiguration.CurrentServiceEndpoint.Address;
        match errs with
          | [] -> ()
          | _ -> 
            errs
            |> List.iter (fun a -> log.Error "%s" a);
            raise (new Exception("There were errors"))

  module internal Entities = 

    let updateStateReq logicalName guid state status = 
      let req = Messages.SetStateRequest()
      req.EntityMoniker <- EntityReference(logicalName, id = guid)
      req.State <- OptionSetValue(state)
      req.Status <- OptionSetValue(status)
      req
    
    let updateState proxy logicalName guid state status = 
      let (proxy : OrganizationServiceProxy) = proxy
      let req = updateStateReq logicalName guid state status
      proxy.Execute(req) :?> Messages.SetStateResponse |> ignore
    
    let assignReq userid logicalName guid = 
      let req = Messages.AssignRequest()
      req.Assignee <- EntityReference("systemuser", id = userid)
      req.Target <- EntityReference(logicalName, id = guid)
      req
    
    let assign proxy userid logicalName guid = 
      let (proxy : OrganizationServiceProxy) = proxy
      let req = assignReq userid logicalName guid
      proxy.Execute(req) :?> Messages.AssignResponse |> ignore
    
    let countHelper proxy logicalName conditions = 
      let em = CrmData.Metadata.entity proxy logicalName
      let conditionToString (att, op, value) = 
        sprintf "<condition attribute='%s' operator='%s' value='%s'/>" att op value
      let conditionString = 
        List.fold (fun state condition -> state + conditionToString condition) 
          String.Empty conditions
      let fetchxml = (sprintf "<fetch mapping='logical' distinct='false' no-lock='true' aggregate='true'>\
           <entity name='%s'>\
             <attribute name='%s' alias='count' aggregate='countcolumn'/>\
                 <filter>\
                   %s
                 </filter>\
           </entity>\
             </fetch>" logicalName em.PrimaryIdAttribute conditionString)
      let fetch = new FetchExpression(fetchxml)
      
      proxy.RetrieveMultiple(fetch).Entities
      |> Seq.head
      |> fun x -> 
        match x.Attributes.Contains("count") with
        | false -> 0
        | true -> x.GetAttributeValue<AliasedValue>("count").Value :?> int
    
    let count proxy logicalName = countHelper proxy logicalName List.Empty
    
    // Why we don't take System Form codes
    // http://www.resultondemand.nl/support/sdk/d14563f7-1fae-4a54-82af-afacf5c8fd56.htm#BKMK_SystemForm
    let countEntities proxy solutionId = 
      let conditions = 
        [ ("solutionid", "eq", string solutionId)
          ("componenttype", "neq", "60") ] 
      countHelper proxy "solutioncomponent" conditions

    // TODO: Ensure that the right system user is returned
    let retrieveSystemUser proxy domainName = 
      let (domainName : string) = domainName
      let ln = @"systemuser"
      let an = @"systemuserid"
      let q = QueryExpression(ln)
      let f = FilterExpression()
      f.AddCondition(ConditionExpression("domainname", ConditionOperator.Equal, domainName))
      q.Criteria <- f
      q.ColumnSet <- ColumnSet(an)
      CrmDataHelper.retrieveFirstMatch proxy q
    
    // Get all entities with a filter
    let internal getEntitiesFilter 
      proxy (logicalName:string)
      (cols:string list) (filter:Map<string,obj>) =
    
      let f = FilterExpression()
      filter |> Map.iter(fun k v -> f.AddCondition(k, ConditionOperator.Equal, v))

      let q = QueryExpression(logicalName)
      if cols.Length = 0 then q.ColumnSet <- ColumnSet(true)
      else q.ColumnSet <- ColumnSet(Array.ofList cols)
      q.Criteria <- f
    
      CrmData.CRUD.retrieveMultiple proxy logicalName q

    let existCrm proxy logicalName guid primaryattribute = 
      let (proxy : OrganizationServiceProxy) = proxy
      let (guid : Guid) = guid
      
      let an = // Limit the amount of network calls
        match primaryattribute with
        | Some v -> v
        | None -> 
          let em = CrmData.Metadata.entity proxy logicalName
          em.PrimaryIdAttribute
      
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, guid))
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(an)
      q.Criteria <- f
      q.NoLock <- true
      CrmData.CRUD.retrieveMultiple proxy logicalName q |> Seq.length
      > 0
    
    let existCrmGuid proxy logicalName filter = 
      let (proxy : OrganizationServiceProxy) = proxy
      let (filter : Map<string, obj>) = filter
      let em = CrmData.Metadata.entity proxy logicalName
      let f = FilterExpression()
      filter 
      |> Map.iter (fun k v -> f.AddCondition(k, ConditionOperator.Equal, v))
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute)
      q.Criteria <- f
      let es = CrmDataHelper.retrieveMultiple proxy q
      
      let guid' = 
        match Seq.length es > 0 with
        | false -> Guid.Empty
        | true -> (Seq.head es).Id
      guid'
    
    let retrieveEntitiesDelta proxy logicalName date = 
      let (date : DateTime) = date
      let f = FilterExpression()
      f.AddCondition
        (@"modifiedon", ConditionOperator.GreaterEqual, date.ToUniversalTime())
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveEntitiesLight proxy logicalName filter = 
      let (filter : Map<string, obj>) = filter
      let em = CrmData.Metadata.entity proxy logicalName
      let f = FilterExpression()
      filter 
      |> Map.iter (fun k v -> f.AddCondition(k, ConditionOperator.Equal, v))
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveAllEntities proxy logicalName = 
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(true)
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveAllEntitiesLight proxy logicalName = 
      let em = CrmData.Metadata.entity proxy logicalName
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveRootBusinessUnit proxy = 
      let ln = @"businessunit"
      let an = @"parentbusinessunitid"
      let an' = @"name"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Null))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(an')
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrieveBusinessUnit proxy name = 
      let (name : string) = name
      let ln = @"businessunit"
      let an = @"name"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, name))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(an)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrievePublisher proxy prefix = 
      let (prefix : string) = prefix
      let ln = @"publisher"
      let an = @"customizationprefix"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, prefix))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(an)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrieveFromView proxy (name: string) user = 
      let ln = 
        match user with
        | true -> @"userquery"
        | false -> @"savedquery"
      
      let f = FilterExpression()
      f.AddCondition(ConditionExpression("name", ConditionOperator.Equal, name))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      
      let fetchxml = 
        CrmDataHelper.retrieveFirstMatch proxy q
        |> fun res -> res.GetAttributeValue<string>("fetchxml")
        |> InternalUtility.decode
      let req = Messages.FetchXmlToQueryExpressionRequest()
      req.FetchXml <- fetchxml
      let resp = 
        proxy.Execute(req) :?> Messages.FetchXmlToQueryExpressionResponse
      CrmDataHelper.retrieveMultiple proxy resp.Query 

    let retrieveSavedQueryReq proxy id status state = 
      let (id : Guid) = id
      let ln = @"savedquery"
      let an = @"savedqueryid"
      let statusC = @"statuscode"
      let stateC = @"statecode"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, id))
      f.AddCondition(ConditionExpression(statusC, ConditionOperator.Equal, status))
      f.AddCondition(ConditionExpression(stateC, ConditionOperator.Equal, state))

      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let createPublisher proxy name display prefix = 
      let (name : String) = name
      let (display : String) = display
      let (prefix : String) = prefix
      let r = Random()
      let e = Entity("publisher")
      e.Attributes.Add("uniquename", name)
      e.Attributes.Add("friendlyname", display)
      e.Attributes.Add("customizationoptionvalueprefix", r.Next(12000, 13000))
      e.Attributes.Add("customizationprefix", prefix)
      CrmData.CRUD.create proxy e (ParameterCollection())
    
    let createSolution proxy name display pubPrefix = 
      let (name : String) = name
      let (display : String) = display
      let (pubPrefix : String) = pubPrefix
      let e = Entity("solution")
      let p = retrievePublisher proxy pubPrefix
      e.Attributes.Add("uniquename", name)
      e.Attributes.Add("friendlyname", display)
      e.Attributes.Add("publisherid", EntityReference("publisher", p.Id))
      e.Attributes.Add("version", "0.0.0.0")
      CrmData.CRUD.create proxy e (ParameterCollection())
    
    let createMany2ManyReq sn r1 r2 = 
      let (sn : string) = sn
      let (r1 : KeyValuePair<string, Guid>) = r1
      let (r2 : KeyValuePair<string, Guid>) = r2
      let request = new AssociateRequest()
      let r1' = new EntityReference(r1.Key.Replace("id", ""), r1.Value)
      let r2' = new EntityReference(r2.Key.Replace("id", ""), r2.Value)
      let ec = new EntityReferenceCollection()
      ec.Add(r2')
      request.Target <- r1'
      request.RelatedEntities <- ec
      request.Relationship <- Relationship(sn)
      request
    
    let createMany2Many proxy sn r1 r2 = 
      let (proxy : OrganizationServiceProxy) = proxy
      let (sn : string) = sn
      let (r1 : KeyValuePair<string, Guid>) = r1
      let (r2 : KeyValuePair<string, Guid>) = r2
      let request = createMany2ManyReq sn r1 r2
      proxy.Execute(request) :?> AssociateResponse

    let retrieveSolution proxy uniqueName columnSet =
      let (uniqueName : string) = uniqueName
      let an = @"uniquename"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression("solution")
      q.ColumnSet <- columnSet
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrieveSolutionId proxy uniqueName =
      ColumnSet(null) |> retrieveSolution proxy uniqueName

    let retrieveSolutionIdAndPrefix proxy uniqueName = 
      ColumnSet("uniquename", "publisherid") |> retrieveSolution proxy uniqueName
      |> fun sol -> 
        let pubId = sol.GetAttributeValue<EntityReference>("publisherid").Id
        let publisher = retrieve proxy "publisher" pubId (RetrieveSelect.Fields ["customizationprefix"])
        sol.Id, publisher.GetAttributeValue<string>("customizationprefix")

    let retrieveSolutionAllAttributes proxy uniqueName = 
      ColumnSet(true) |> retrieveSolution proxy uniqueName 
    
    let retrieveSolutionEntities proxy solutionName =
      let solutionFilter = [("uniquename", solutionName)] |> Map.ofList
      let solutions = 
        getEntitiesFilter proxy "solution" 
          ["solutionid"; "uniquename"] solutionFilter
    
      solutions
      |> Seq.map (fun sol ->
        let solutionComponentFilter = 
          [ ("solutionid", sol.Attributes.["solutionid"]) 
            ("componenttype", 1 :> obj) // 1 = Entity
          ] |> Map.ofList

        getEntitiesFilter proxy "solutioncomponent" 
          ["solutionid"; "objectid"; "componenttype"] solutionComponentFilter
        |> Seq.map (fun sc -> 
          CrmData.Metadata.getEntityLogicalNameFromId proxy (sc.Attributes.["objectid"] :?> Guid))
      )
      |> Seq.concat
    
    let retrieveImportJobHelper proxy importJobId includeXML = 
      let (importJobId : Guid) = importJobId
      let ln = @"importjob"
      let ans = [ @"progress"; @"completedon" ]
      let ans' = 
        match includeXML with
        | true -> @"data" :: ans
        | false -> ans
        |> List.toArray
      
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (@"importjobid", ConditionOperator.Equal, importJobId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(ans')
      q.Criteria <- f
      q.NoLock <- true
      CrmDataHelper.retrieveFirstMatch proxy q

    let retrieveImportJob proxy importJobId = 
      retrieveImportJobHelper proxy importJobId false

    let retrieveImportJobWithXML proxy importJobId = 
      retrieveImportJobHelper proxy importJobId true
    
    let retrievePluginType proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"plugintype"
      let em = CrmData.Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let tryRetrievePluginType proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"plugintype"
      let em = CrmData.Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrievePluginTypes proxy assemblyId = 
      let (assemblyId : Guid) = assemblyId
      let ln = @"plugintype"
      let an = @"pluginassemblyid"
      let em = CrmData.Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression(an, ConditionOperator.Equal, assemblyId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveSdkMessage proxy eventOperation = 
      let (eventOperation : string) = eventOperation
      let ln = @"sdkmessage"
      let em = CrmData.Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, eventOperation))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrieveSdkProcessingStep proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"sdkmessageprocessingstep"
      let em = CrmData.Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let tryRetrieveSdkProcessingStep proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"sdkmessageprocessingstep"
      let em = CrmData.Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrieveSdkMessageFilter proxy primaryObjectType sdkMessageId = 
      let (primaryObjectType : string) = primaryObjectType
      let (sdkMessageId : Guid) = sdkMessageId
      let ln = @"sdkmessagefilter"
      let em = CrmData.Metadata.entity proxy ln
      let f = FilterExpression()
      match String.IsNullOrEmpty(primaryObjectType) with
      | true -> ()
      | false -> 
        f.AddCondition
          (ConditionExpression
             (@"primaryobjecttypecode", ConditionOperator.Equal, 
              primaryObjectType))
      f.AddCondition
        (ConditionExpression
           (@"sdkmessageid", ConditionOperator.Equal, sdkMessageId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute)
      q.Criteria <- f 
      CrmDataHelper.retrieveFirstMatch proxy q
    
    let retrieveWebResources proxy solutionId = 
      let (solutionId : Guid) = solutionId
      let ln = @"webresource"
      let an = @"solutionid"
      let nm = @"ismanaged"
      let le = LinkEntity()
      le.JoinOperator <- JoinOperator.Inner
      le.LinkFromAttributeName <- @"webresourceid"
      le.LinkFromEntityName <- @"webresource"
      le.LinkToAttributeName <- @"objectid"
      le.LinkToEntityName <- @"solutioncomponent"
      le.LinkCriteria.Conditions.Add
        (ConditionExpression(an, ConditionOperator.Equal, solutionId))
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression(nm, ConditionOperator.Equal, false))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      q.LinkEntities.Add(le)
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrievePluginAssembly proxy uniqueName = 
      let (uniqueName : String) = uniqueName
      let ln = @"pluginassembly"
      let an = @"name"
      let em = CrmData.Metadata.entity proxy ln
      let le = LinkEntity()
      le.JoinOperator <- JoinOperator.LeftOuter
      le.LinkFromAttributeName <- @"pluginassemblyid"
      le.LinkFromEntityName <- @"pluginassembly"
      le.LinkToAttributeName <- @"objectid"
      le.LinkToEntityName <- @"solutioncomponent"
      le.Columns.AddColumn("solutionid")
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression(an, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet
                       (em.PrimaryIdAttribute, em.PrimaryNameAttribute, 
                        "sourcehash")
      q.Criteria <- f
      q.LinkEntities.Add(le)
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrievePluginAssemblies proxy solutionId = 
      let (solutionId : Guid) = solutionId
      let ln = @"pluginassembly"
      let an = @"solutionid"
      let em = CrmData.Metadata.entity proxy ln
      let le = LinkEntity()
      le.JoinOperator <- JoinOperator.Inner
      le.LinkFromAttributeName <- @"pluginassemblyid"
      le.LinkFromEntityName <- @"pluginassembly"
      le.LinkToAttributeName <- @"objectid"
      le.LinkToEntityName <- @"solutioncomponent"
      le.LinkCriteria.Conditions.Add
        (ConditionExpression(an, ConditionOperator.Equal, solutionId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet
                       (em.PrimaryIdAttribute, em.PrimaryNameAttribute,
                        "sourcehash", "isolationmode")
      q.LinkEntities.Add(le)
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrievePluginProcessingSteps proxy typeId = 
      let (typeId : Guid) = typeId
      let ln = @"sdkmessageprocessingstep"
      let an = @"solutionid"
      let an' = @"plugintypeid"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an', ConditionOperator.Equal, typeId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveAllPluginProcessingSteps proxy solutionId = 
      let (solutionId : Guid) = solutionId
      let ln = @"sdkmessageprocessingstep"
      let an = @"solutionid"
      //let em = CrmData.Metadata.entity proxy ln
      let le = LinkEntity()
      le.JoinOperator <- JoinOperator.Inner
      le.LinkFromAttributeName <- @"sdkmessageprocessingstepid"
      le.LinkFromEntityName <- @"sdkmessageprocessingstep"
      le.LinkToAttributeName <- @"objectid"
      le.LinkToEntityName <- @"solutioncomponent"
      le.LinkCriteria.Conditions.Add
        (ConditionExpression(an, ConditionOperator.Equal, solutionId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.LinkEntities.Add(le)
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrievePluginProcessingStepImages proxy stepId = 
      let (stepId : Guid) = stepId
      let ln = @"sdkmessageprocessingstepimage"
      let an' = @"sdkmessageprocessingstepid"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an', ConditionOperator.Equal, stepId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveWorkflows proxy solutionId = 
      let (solutionId : Guid) = solutionId
      let ln = @"workflow"
      let an = @"solutionid"
      let t = @"type"
      let le = LinkEntity()
      le.JoinOperator <- JoinOperator.Inner
      le.LinkFromAttributeName <- @"workflowid"
      le.LinkFromEntityName <- @"workflow"
      le.LinkToAttributeName <- @"objectid"
      le.LinkToEntityName <- @"solutioncomponent"
      le.LinkCriteria.Conditions.Add
        (ConditionExpression(an, ConditionOperator.Equal, solutionId))
      let f = FilterExpression()
      // Only definition workflows
      f.AddCondition(ConditionExpression(t, ConditionOperator.Equal, 1))
      // HACK: this is currently broken! Exclude "modern flow" from extended solution.
      f.AddCondition(ConditionExpression("category", ConditionOperator.NotEqual, 5))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.LinkEntities.Add(le)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveWorkflowsOfStatus proxy solutionId status = 
      let (solutionId : Guid) = solutionId
      let (status : int) = status
      let ln = @"workflow"
      let an = @"solutionid"
      let t = @"type"
      let sc = @"statuscode"
      let em = CrmData.Metadata.entity proxy ln
      let le = LinkEntity()
      le.JoinOperator <- JoinOperator.Inner
      le.LinkFromAttributeName <- @"workflowid"
      le.LinkFromEntityName <- @"workflow"
      le.LinkToAttributeName <- @"objectid"
      le.LinkToEntityName <- @"solutioncomponent"
      le.LinkCriteria.Conditions.Add
        (ConditionExpression(an, ConditionOperator.Equal, solutionId))
      let f = FilterExpression()
      // Only definition workflows
      f.AddCondition(ConditionExpression(t, ConditionOperator.Equal, 1))
      // draft (1) or published (2) workflow
      f.AddCondition(ConditionExpression(sc, ConditionOperator.Equal, status))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.LinkEntities.Add(le)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
    
    let retrieveAllSolutionComponenets proxy solutionId =
      let (solutionId : Guid) = solutionId
      let ln = @"solutioncomponent"
      let an = @"solutionid"
      let le = LinkEntity()
      le.JoinOperator <- JoinOperator.Inner
      le.LinkFromAttributeName <- @"solutionid"
      le.LinkFromEntityName <- @"solutioncomponent"
      le.LinkToAttributeName <- @"solutionid"
      le.LinkToEntityName <- @"solution"
      le.LinkCriteria.Conditions.Add
        (ConditionExpression(an, ConditionOperator.Equal, solutionId))
      let f = FilterExpression()
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.LinkEntities.Add(le)
      q.Criteria <- f
      CrmDataHelper.retrieveMultiple proxy q
