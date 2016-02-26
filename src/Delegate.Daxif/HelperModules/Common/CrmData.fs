namespace DG.Daxif.HelperModules.Common

open System
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Query
open DG.Daxif
open DG.Daxif.HelperModules.Common.Utility

// http://msdn.microsoft.com/en-us/library/microsoft.xrm.sdk.organizationrequest.requestname.aspx
module internal CrmData = 
  module internal Info = 
    let private versionHelper v = 
      match v |> Seq.head with
      | '5' -> CrmReleases.CRM2011
      | '6' -> CrmReleases.CRM2013
      | '7' -> CrmReleases.CRM2015
      | '8' -> CrmReleases.CRM2016
      | _ -> failwith "Version not supported."
    
    let version (proxy : OrganizationServiceProxy) = 
      let req = Messages.RetrieveVersionRequest()
      let resp = proxy.Execute(req) :?> Messages.RetrieveVersionResponse
      resp.Version, resp.Version |> versionHelper
  
  module internal Metadata = 
    let private entityHelper proxy (logicalName:string) filter = 
      let (proxy : OrganizationServiceProxy) = proxy
      let (filter : EntityFilters) = filter
      let req = OrganizationRequest()
      let param = ParameterCollection()
      param.Add(@"LogicalName", logicalName)
      param.Add(@"EntityFilters", filter)
      param.Add(@"MetadataId", Guid.Empty)
      param.Add(@"RetrieveAsIfPublished", true)
      req.RequestName <- @"RetrieveEntity"
      req.Parameters.AddRange(param)
      let resp = proxy.Execute(req)
      (Seq.head resp.Results).Value :?> EntityMetadata
    
    let entity proxy logicalName = 
      entityHelper proxy logicalName EntityFilters.Entity
    let entityAll proxy logicalName = 
      entityHelper proxy logicalName EntityFilters.All
    let entityAttributes proxy logicalName = 
      (entityHelper proxy logicalName EntityFilters.Attributes).Attributes
    let entityOneToManyRelationships proxy logicalName = 
      (entityHelper proxy logicalName EntityFilters.Relationships).OneToManyRelationships
    let entityManyToOneRelationships proxy logicalName = 
      (entityHelper proxy logicalName EntityFilters.Relationships).ManyToOneRelationships
    let entityManyToManyRelationships proxy logicalName = 
      (entityHelper proxy logicalName EntityFilters.Relationships).ManyToManyRelationships
    
    let allEntities (proxy : OrganizationServiceProxy) = 
      let req = OrganizationRequest()
      let param = ParameterCollection()
      param.Add(@"EntityFilters", EntityFilters.Entity)
      param.Add(@"RetrieveAsIfPublished", true)
      req.RequestName <- @"RetrieveAllEntities"
      req.Parameters.AddRange(param)
      proxy.Timeout <- new TimeSpan(0, 10, 0) // 10 minutes timeout
      let resp = proxy.Execute(req)
      (Seq.head resp.Results).Value :?> seq<EntityMetadata>
  
  module internal CRUD = 
    let createReq entity parameters = 
      let (parameters : ParameterCollection) = parameters
      let req = Messages.CreateRequest()
      req.Target <- entity
      req.Parameters.AddRange(parameters)
      req
    
    let create proxy entity parameters = 
      let (proxy : OrganizationServiceProxy) = proxy
      let (parameters : ParameterCollection) = parameters
      let req = createReq entity parameters
      let resp = proxy.Execute(req) :?> Messages.CreateResponse
      resp.id
    
    let retrieveReq logicalName guid = 
      let req = Messages.RetrieveRequest()
      req.ColumnSet <- ColumnSet(true)
      req.Target <- EntityReference(logicalName, id = guid)
      req
    
    let retrieve proxy logicalName guid = 
      let (proxy : OrganizationServiceProxy) = proxy
      let req = retrieveReq logicalName guid
      let resp = proxy.Execute(req) :?> Messages.RetrieveResponse
      resp.Entity
    
    let retrieveMultiple proxy logicalName query = 
      try 
        let (proxy : OrganizationServiceProxy) = proxy
        let (query : QueryExpression) = query
        query.PageInfo <- PagingInfo()
        query.PageInfo.ReturnTotalRecordCount <- true
        query.PageInfo.PageNumber <- 1
        query.PageInfo.Count <- 1000 // ItemsInEachCall 
        seq { 
          let resp = proxy.RetrieveMultiple(query)
          yield! resp.Entities
          let rec retrieveMultiple' (ec : EntityCollection) pn = 
            seq { 
              match ec.MoreRecords with
              | true -> 
                query.PageInfo.PageNumber <- (pn + 1)
                query.PageInfo.PagingCookie <- ec.PagingCookie
                proxy.Timeout <- new TimeSpan(0, 10, 0) // 10 minutes timeout
                let resp' = proxy.RetrieveMultiple(query)
                yield! resp'.Entities
                yield! retrieveMultiple' resp' (pn + 1)
              | false -> ()
            }
          yield! retrieveMultiple' resp 1
        }
      with ex -> 
        failwith 
          ("Retrieving " + logicalName + " failed: " 
           + Utility.getFullException (ex))
    
    let updateReq entity = 
      let req = Messages.UpdateRequest()
      req.Target <- entity
      req
    
    let update proxy entity = 
      let (proxy : OrganizationServiceProxy) = proxy
      let req = updateReq entity
      proxy.Execute(req) :?> Messages.UpdateResponse |> ignore
      entity.Id
    
    let deleteReq logicalName guid = 
      let req = Messages.DeleteRequest()
      req.Target <- EntityReference(logicalName, id = guid)
      req
    
    let delete proxy logicalName guid = 
      let (proxy : OrganizationServiceProxy) = proxy
      let req = deleteReq logicalName guid
      proxy.Execute(req) :?> Messages.DeleteResponse |> ignore
      Guid.Empty
    
    let publish proxy = 
      let (proxy : OrganizationServiceProxy) = proxy
      let req = Messages.PublishAllXmlRequest()
      proxy.Timeout <- new TimeSpan(1, 0, 0) // 1 hour timeout for PublishAll
      proxy.Execute(req) :?> Messages.PublishAllXmlResponse |> ignore
  
  module internal Entities = 
    let seqTryHead' s = Seq.tryPick Some s
    
    let seqTryHead logicName entityName (s : seq<'a>) : 'a = 
      match s |> seqTryHead' with
      | Some v -> v
      | None -> 
        let potentialEntityName = 
          match String.IsNullOrEmpty(entityName) with
          | true -> ""
          | false -> ", " + entityName
        (logicName, potentialEntityName)
        ||> sprintf 
              "Failed to retrieve %s %s. No entitiy with that name in CRM."
        |> failwith
    
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
      let em = Metadata.entity proxy logicalName
      let conditionToString (att, op, value) = 
        sprintf "<condition attribute='%s' operator='%s' value='%s'/>" att op value
      let conditionString = 
        List.fold (fun state condition -> state + conditionToString condition) 
          String.Empty conditions
      let fetchxml = (sprintf "<fetch mapping='logical' distinct='false' no-lock='true' aggregate='true'>\
           <entity name='%s'>\
             <attribute name='%s' alias='count' aggregate='count'/>\
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
      f.AddCondition
        (ConditionExpression("domainname", ConditionOperator.Equal, domainName))
      q.Criteria <- f
      q.ColumnSet <- ColumnSet(an)
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln domainName
    
    let existCrm proxy logicalName guid primaryattribute = 
      let (proxy : OrganizationServiceProxy) = proxy
      let (guid : Guid) = guid
      
      let an = // Limit the amount of network calls
        match primaryattribute with
        | Some v -> v
        | None -> 
          let em = Metadata.entity proxy logicalName
          em.PrimaryIdAttribute
      
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, guid))
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(an)
      q.Criteria <- f
      q.NoLock <- true
      CRUD.retrieveMultiple proxy logicalName q |> Seq.length
      > 0
    
    let existCrmGuid proxy logicalName filter = 
      let (proxy : OrganizationServiceProxy) = proxy
      let (filter : Map<string, obj>) = filter
      let em = Metadata.entity proxy logicalName
      let f = FilterExpression()
      filter 
      |> Map.iter (fun k v -> f.AddCondition(k, ConditionOperator.Equal, v))
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute)
      q.Criteria <- f
      let es = (CRUD.retrieveMultiple proxy logicalName q)
      
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
      CRUD.retrieveMultiple proxy logicalName q
    
    let retrieveEntitiesLight proxy logicalName filter = 
      let (filter : Map<string, obj>) = filter
      let em = Metadata.entity proxy logicalName
      let f = FilterExpression()
      filter 
      |> Map.iter (fun k v -> f.AddCondition(k, ConditionOperator.Equal, v))
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy logicalName q
    
    let retrieveAllEntities proxy logicalName = 
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(true)
      CRUD.retrieveMultiple proxy logicalName q
    
    let retrieveAllEntitiesLight proxy logicalName = 
      let em = Metadata.entity proxy logicalName
      let q = QueryExpression(logicalName)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      CRUD.retrieveMultiple proxy logicalName q
    
    let retrieveRootBusinessUnit proxy = 
      let ln = @"businessunit"
      let an = @"parentbusinessunitid"
      let an' = @"name"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Null))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(an')
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln ""
    
    let retrieveBusinessUnit proxy name = 
      let (name : string) = name
      let ln = @"businessunit"
      let an = @"name"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, name))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(an)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln name
    
    let retrievePublisher proxy prefix = 
      let (prefix : string) = prefix
      let ln = @"publisher"
      let an = @"customizationprefix"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, prefix))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(an)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln prefix
    
    let retrieveFromView proxy name user = 
      let (name : string) = name
      
      let ln = 
        match user with
        | true -> @"userquery"
        | false -> @"savedquery"
      
      let an = @"name"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, name))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q
      |> seqTryHead ln name
      |> fun x -> 
        let fetchxml = 
          x.Attributes.["fetchxml"] :?> string 
          |> DG.Daxif.HelperModules.Common.Utility.decode
        let req = Messages.FetchXmlToQueryExpressionRequest()
        req.FetchXml <- fetchxml
        let resp = 
          proxy.Execute(req) :?> Messages.FetchXmlToQueryExpressionResponse
        resp.Query |> fun q -> CRUD.retrieveMultiple proxy ln q
    
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
      CRUD.create proxy e (ParameterCollection())
    
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
      CRUD.create proxy e (ParameterCollection())
    
    let createMany2ManyReq sn r1 r2 = 
      let (sn : string) = sn
      let (r1 : keyValuePair<string, Guid>) = r1
      let (r2 : keyValuePair<string, Guid>) = r2
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
      let (r1 : keyValuePair<string, Guid>) = r1
      let (r2 : keyValuePair<string, Guid>) = r2
      let request = createMany2ManyReq sn r1 r2
      proxy.Execute(request) :?> AssociateResponse
    
    let retrieveSolution proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"solution"
      let an = @"uniquename"
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression(an, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(an)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln uniqueName
    
    let retrieveImportJob proxy importJobId = 
      let (importJobId : Guid) = importJobId
      let ln = @"importjob"
      let ans = [ @"progress"; @"completedon" ] |> List.toArray
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (@"importjobid", ConditionOperator.Equal, importJobId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(ans)
      q.Criteria <- f
      q.NoLock <- true
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln (importJobId.ToString())
    
    let retrievePluginType proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"plugintype"
      let em = Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln uniqueName
    
    let tryRetrievePluginType proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"plugintype"
      let em = Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead'
    
    let retrievePluginTypes proxy assemblyId = 
      let (assemblyId : Guid) = assemblyId
      let ln = @"plugintype"
      let an = @"pluginassemblyid"
      let em = Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression(an, ConditionOperator.Equal, assemblyId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q
    
    let retrieveSdkMessage proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"sdkmessage"
      let em = Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln uniqueName
    
    let retrieveSdkProcessingStep proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"sdkmessageprocessingstep"
      let em = Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead ln uniqueName
    
    let tryRetrieveSdkProcessingStep proxy uniqueName = 
      let (uniqueName : string) = uniqueName
      let ln = @"sdkmessageprocessingstep"
      let em = Metadata.entity proxy ln
      let f = FilterExpression()
      f.AddCondition
        (ConditionExpression
           (em.PrimaryNameAttribute, ConditionOperator.Equal, uniqueName))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(em.PrimaryIdAttribute, em.PrimaryNameAttribute)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q |> seqTryHead'
    
    let retrieveSdkMessageFilter proxy primaryObjectType sdkMessageId = 
      let (primaryObjectType : string) = primaryObjectType
      let (sdkMessageId : Guid) = sdkMessageId
      let ln = @"sdkmessagefilter"
      let em = Metadata.entity proxy ln
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
      CRUD.retrieveMultiple proxy ln q 
      |> seqTryHead ln (sdkMessageId.ToString())
    
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
        (ConditionExpression(@"ismanaged", ConditionOperator.Equal, false))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      q.LinkEntities.Add(le)
      CRUD.retrieveMultiple proxy ln q
    
    let retrievePluginAssembly proxy uniqueName = 
      let (uniqueName : String) = uniqueName
      let ln = @"pluginassembly"
      let an = @"name"
      let em = Metadata.entity proxy ln
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
      CRUD.retrieveMultiple proxy ln q
    
    let retrievePluginAssemblies proxy solutionId = 
      let (solutionId : Guid) = solutionId
      let ln = @"pluginassembly"
      let an = @"solutionid"
      let em = Metadata.entity proxy ln
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
                        "sourcehash")
      q.LinkEntities.Add(le)
      CRUD.retrieveMultiple proxy ln q
    
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
      CRUD.retrieveMultiple proxy ln q
    
    let retrieveAllPluginProcessingSteps proxy solutionId = 
      let (solutionId : Guid) = solutionId
      let ln = @"sdkmessageprocessingstep"
      let an = @"solutionid"
      let em = Metadata.entity proxy ln
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
      CRUD.retrieveMultiple proxy ln q
    
    let retrievePluginProcessingStepImages proxy stepId = 
      let (stepId : Guid) = stepId
      let ln = @"sdkmessageprocessingstepimage"
      let an' = @"sdkmessageprocessingstepid"
      let f = FilterExpression()
      f.AddCondition(ConditionExpression(an', ConditionOperator.Equal, stepId))
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q
    
    let retrieveWorkflows proxy solutionId = 
      let (solutionId : Guid) = solutionId
      let ln = @"workflow"
      let an = @"solutionid"
      let t = @"type"
      let sc = @"statuscode"
      let em = Metadata.entity proxy ln
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
      let q = QueryExpression(ln)
      q.ColumnSet <- ColumnSet(true)
      q.LinkEntities.Add(le)
      q.Criteria <- f
      CRUD.retrieveMultiple proxy ln q
    
    let retrieveWorkflowsOfStatus proxy solutionId status = 
      let (solutionId : Guid) = solutionId
      let (status : int) = status
      let ln = @"workflow"
      let an = @"solutionid"
      let t = @"type"
      let sc = @"statuscode"
      let em = Metadata.entity proxy ln
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
      CRUD.retrieveMultiple proxy ln q
