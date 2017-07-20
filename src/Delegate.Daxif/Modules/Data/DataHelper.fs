﻿module internal DG.Daxif.Modules.Data.DataHelper

open System
open System.IO
open System.ServiceProcess
open System.Collections.Generic
open System.Runtime.InteropServices
open Microsoft.FSharp.Core
open Microsoft.Crm.Sdk.Messages
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Query
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Modules.Serialization

let throttle = function
  | AuthenticationProviderType.OnlineFederation -> 1
  | _ -> 100

// https://msdn.microsoft.com/en-us/library/jj863631.aspx
// No specified limit on-premise but experience says there is
let maxConcurrentMultipleRequest = function
  | AuthenticationProviderType.OnlineFederation -> 2
  | _ -> 10 //Soft limit

// https://msdn.microsoft.com/en-us/library/jj863631.aspx
let maxRequestInRequestCollection = 1000
    
/// TODO:
let exists' org ac entityName filter (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  CrmDataInternal.Entities.existCrmGuid p entityName filter
    
let count' org ac entityNames (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  entityNames 
  |> Array.Parallel.iter (fun en -> 
        use p = ServiceProxy.getOrganizationServiceProxy m tc
        try 
          log.WriteLine
            (LogLevel.Verbose, 
            sprintf "%s: %i record(s)" en (CrmDataInternal.Entities.count p en))
        with ex -> 
          log.WriteLine(LogLevel.Warning, sprintf "%s: %s" en ex.Message))

// @deprecated
let updateState' org ac entityName filter (state, status) 
    (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  // TODO: Would be nice with TypeProviders based on target system
  CrmDataInternal.Entities.retrieveEntitiesLight p entityName filter
  |> FSharpCoreExt.Seq.split (1000 * (throttle m.AuthenticationType))
  |> Seq.iter 
        (fun xs -> 
        xs 
        |> Array.Parallel.iter (fun e -> 
            use p' = ServiceProxy.getOrganizationServiceProxy m tc
            let en' = entityName
            let ei' = e.Id.ToString()
            try 
              CrmDataInternal.Entities.updateState p' en' e.Id state status
              log.WriteLine
                (LogLevel.Verbose, sprintf "%s:%s state was updated" en' ei')
            with ex -> 
              log.WriteLine
                (LogLevel.Warning, sprintf "%s:%s %s" en' ei' ex.Message)))

/// TODO:
let updateState'' org ac entityName filter (state, status) 
    (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  CrmDataInternal.Entities.retrieveEntitiesLight p entityName filter
  |> FSharpCoreExt.Seq.split (maxRequestInRequestCollection * (maxConcurrentMultipleRequest m.AuthenticationType))
  |> Seq.iter (fun xs -> 
        xs
        |> Array.toSeq
        |> FSharpCoreExt.Seq.split maxRequestInRequestCollection
        |> Seq.toArray
        |> Array.Parallel.map (fun es -> 
            let em = new ExecuteMultipleRequest()
            em.Settings <- new ExecuteMultipleSettings()
            em.Settings.ContinueOnError <- true
            em.Settings.ReturnResponses <- true
            em.Requests <- new OrganizationRequestCollection()
            es
            |> Array.Parallel.map (fun e -> 
                  CrmDataInternal.Entities.updateStateReq entityName e.Id state status :> OrganizationRequest ) 
            |> em.Requests.AddRange //Array.iter (fun x -> em.Requests.Add(x)) // OrganizationRequestCollection is not thread-safe
            em, es)
        |> Array.Parallel.map (fun (em, es) -> 
            try 
              use p' = ServiceProxy.getOrganizationServiceProxy m tc
              (p'.Execute(em) :?> ExecuteMultipleResponse, es) |> Some
            with ex -> 
              log.WriteLine(LogLevel.Warning, sprintf "%s" ex.Message)
              None)
        |> Array.Parallel.choose (id)
        |> Array.Parallel.iter (fun (em, es) -> 
            em.Responses
            |> Seq.toArray
            |> Array.Parallel.iter 
                  (fun kv -> 
                  let e = es.[kv.RequestIndex]
                  let en = e.LogicalName
                  let ei = e.Id
                  let ei' = ei.ToString()
                  match kv.Fault with
                  | null -> 
                    log.WriteLine
                      (LogLevel.Verbose, sprintf "%s:%s state was updated" en ei')
                  | fault -> 
                    log.WriteLine
                      (LogLevel.Warning, sprintf "%s:%s %s" en ei' fault.Message))))


// @deprecated
let reassignAllRecords' org ac userFrom userTo 
    (log : ConsoleLogger) = 
  let (userFrom : Guid) = userFrom
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  CrmData.Metadata.allEntities p
  |> Seq.filter (fun x -> 
        let ot = 
          match x.OwnershipType.HasValue with
          | false -> OwnershipTypes.None
          | true -> x.OwnershipType.Value
        ot = OwnershipTypes.UserOwned)
  |> Seq.map (fun x -> x.LogicalName)
  |> Seq.toArray
  |> Array.Parallel.iter (fun y -> 
        use p' = ServiceProxy.getOrganizationServiceProxy m tc
        CrmDataInternal.Entities.retrieveEntitiesLight p' y 
          (Map.empty |> Map.add (@"ownerid") (userFrom :> obj))
        |> Seq.toArray
        |> Array.Parallel.iter (fun z -> 
            use p'' = ServiceProxy.getOrganizationServiceProxy m tc
            let en' = y
            let ei = z.Id
            let ei' = ei.ToString()
            try 
              CrmDataInternal.Entities.assign p'' userTo en' ei
              log.WriteLine
                (LogLevel.Verbose, 
                  sprintf "%s:%s record was assigned from:%A to:%A " en' ei' 
                    userFrom userTo)
            with ex -> 
              log.WriteLine
                (LogLevel.Warning, sprintf "%s:%s %s" en' ei' ex.Message)))

/// TODO:
let reassignAllRecords'' org ac userFrom userTo 
    (log : ConsoleLogger) = 
  let (userFrom : Guid) = userFrom
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  CrmData.Metadata.allEntities p
  |> Seq.filter (fun x -> 
        let ot = 
          match x.OwnershipType.HasValue with
          | false -> OwnershipTypes.None
          | true -> x.OwnershipType.Value
        ot = OwnershipTypes.UserOwned)
  |> Seq.map (fun x -> x.LogicalName)
  |> Seq.iter (fun entityName -> 
        use p' = ServiceProxy.getOrganizationServiceProxy m tc
        let filter = (Map.empty |> Map.add (@"ownerid") (userFrom :> obj))
        CrmDataInternal.Entities.retrieveEntitiesLight p' entityName filter
        |> FSharpCoreExt.Seq.split (maxRequestInRequestCollection * (maxConcurrentMultipleRequest m.AuthenticationType))
        |> Seq.iter (fun xs -> 
            xs
            |> Array.toSeq
            |> FSharpCoreExt.Seq.split maxRequestInRequestCollection
            |> Seq.toArray
            |> Array.Parallel.map (fun es -> 
                  let em = new ExecuteMultipleRequest()
                  em.Settings <- new ExecuteMultipleSettings()
                  em.Settings.ContinueOnError <- true
                  em.Settings.ReturnResponses <- true
                  em.Requests <- new OrganizationRequestCollection()
                  es
                  |> Array.Parallel.map (fun e -> 
                      CrmDataInternal.Entities.assignReq userTo entityName e.Id :> OrganizationRequest ) 
                  |> em.Requests.AddRange // Array.iter (fun x -> em.Requests.Add(x)) // OrganizationRequestCollection is not thread-safe
                  em, es)
            |> Array.Parallel.map (fun (em, es) -> 
                  try 
                    use p' = ServiceProxy.getOrganizationServiceProxy m tc
                    (p'.Execute(em) :?> ExecuteMultipleResponse, es) |> Some
                  with ex -> 
                    log.WriteLine(LogLevel.Warning, sprintf "%s" ex.Message)
                    None)
            |> Array.Parallel.choose (id)
            |> Array.Parallel.iter (fun (em, es) -> 
                  em.Responses
                  |> Seq.toArray
                  |> Array.Parallel.iter (fun kv -> 
                      let e = es.[kv.RequestIndex]
                      let en = e.LogicalName
                      let ei = e.Id
                      let ei' = ei.ToString()
                      match kv.Fault with
                      | null -> 
                        log.WriteLine
                          (LogLevel.Verbose, 
                            sprintf "%s:%s record was assigned from:%A to:%A " en 
                              ei' userFrom userTo)
                      | fault -> 
                        log.WriteLine
                          (LogLevel.Warning, 
                            sprintf "%s:%s %s" en ei' fault.Message)))))


/// TODO:
let export' org ac location entityNames (log : ConsoleLogger) 
    serialize = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  ensureDirectory location
  let ext = (@"." + Utility.unionToString serialize).ToLower()
  entityNames 
  |> Array.Parallel.iter (fun en -> 
        use p = ServiceProxy.getOrganizationServiceProxy m tc
        let eLoc = location + en
        ensureDirectory eLoc
        CrmDataInternal.Entities.retrieveAllEntities p en
        |> FSharpCoreExt.Seq.split (1000 * (throttle m.AuthenticationType))
        |> Seq.iter (fun xs -> 
            xs 
            |> Array.Parallel.iter (fun e -> 
                  let ei = e.Id
                  let ei' = ei.ToString()
                  let file = 
                    sprintf "%s%c%s_%s.%s" eLoc Path.DirectorySeparatorChar 
                      (Utility.timeStamp'()) // TODO: Should be replaced with modifiedon datetime
                                            ei' ext
                  let body = 
                    SerializationHelper.serializeObjectToBytes<Entity> serialize 
                      e
                  File.WriteAllBytes(file, body)
                  log.WriteLine
                    (LogLevel.Verbose, sprintf "%s:%s was saved to disk" en ei'))))


/// TODO:
let exportDelta' org ac location entityNames date (log:ConsoleLogger) serialize =
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)

  ensureDirectory location

  entityNames 
  |> Array.Parallel.iter(fun en ->
    use p = ServiceProxy.getOrganizationServiceProxy m tc
    let eLoc = location + en

    ensureDirectory eLoc

    CrmDataInternal.Entities.retrieveEntitiesDelta p en date
    |> FSharpCoreExt.Seq.split (1000 * (throttle m.AuthenticationType))
    |> Seq.iter(fun xs ->
      xs
      |> Array.Parallel.iter(fun e ->
        let ei = e.Id
        let ei' = ei.ToString()
        let file = 
          sprintf "%s%c%s_%s.%s" 
            eLoc
            Path.DirectorySeparatorChar
            (Utility.timeStamp' ())
            ei'
            (serialize.ToString().ToLower())

        let body =
          SerializationHelper.serializeObjectToBytes<Entity>
            serialize e
        File.WriteAllBytes(file, body)
                    
        log.WriteLine(LogLevel.Verbose, 
          sprintf "%s:%s was saved to disk" en ei'))))


/// TODO:
let exportView' org ac location view user (log:ConsoleLogger) serialize =
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)

  use p = ServiceProxy.getOrganizationServiceProxy m tc

  CrmDataInternal.Entities.retrieveFromView p view user
  |> Seq.toArray
  |> Array.Parallel.iter(fun e ->
    let en = e.LogicalName
    let eLoc = location + en

    ensureDirectory eLoc
    
    let ei = e.Id
    let ei' = ei.ToString()

    let file = 
      sprintf "%s%c%s_%s.%s" 
        eLoc
        Path.DirectorySeparatorChar
        (Utility.timeStamp' ())
        ei'
        ((Utility.unionToString serialize).ToString())

    let body = 
      SerializationHelper.serializeObjectToBytes<Entity>
        serialize e
    File.WriteAllBytes(file, body)
                    
    log.WriteLine(LogLevel.Verbose, 
      sprintf "%s:%s was saved to disk" en ei'))


// @deprecated
let import' org ac location (log:ConsoleLogger) serialize attribs data =
    let imported = location + @"..\imported\"

    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)

    let i = Activator.CreateInstance<Entity>()
    let t = Type.GetType (i.GetType().AssemblyQualifiedName)

    match Directory.Exists(location) with
    | false -> ()
    | true ->
        let ext = (@"." + Utility.unionToString serialize).ToLower()

        // Refactor mCache memoization (concurrent thread-safe)
        let dCache = 
          let dCacheHelper (proxy,entityname) =
            CrmData.Metadata.entity proxy entityname
          memoizeConcurrent' dCacheHelper

        // For each entity in location, populate EntityMetadata
        Directory.GetDirectories(location)
        |> Array.map(fun x ->
            let x' = Path.GetFileName(x)
            dCache(ServiceProxy.getOrganizationServiceProxy m tc, x'))
        |> ignore

        // Add memoization to avoid to many network queries (concurrent thread-safe)
        let dMem = 
            let dMemHelper (proxy,(entityname,entityid,primaryattribute)) =
                CrmDataInternal.Entities.existCrm proxy entityname entityid primaryattribute
            memoizeConcurrent' dMemHelper

        Directory.GetDirectories(location)
        |> Array.iter(fun location' ->
          Directory.EnumerateFiles(location', "*" + ext, SearchOption.AllDirectories)
          |> Seq.toArray
          |> Array.Parallel.iter(fun f ->
              use p = ServiceProxy.getOrganizationServiceProxy m tc
              let e = SerializationHelper.deserializeFileToObject<Entity>
                        serialize f

              let en = e.LogicalName
              let em = dCache(p, en)
              let ei = e.Id
              let ei' = ei.ToString()

              // check for related entities if they are in target else remove
              let a'' = 
                  e.Attributes 
                  |> Seq.fold(fun (a:AttributeCollection) x -> 
                      match x.Value with
                      | :? EntityReference as er ->
                          let ern = er.LogicalName
                          let erm = dCache(p, ern)
                          let eri = dMapLookup data ern er.Id ?| er.Id
                               
                          let eri' = eri.ToString()

                          let et = dMem (p, (ern, eri, erm.PrimaryIdAttribute |> Some))

                          match et with
                          | false -> 
                              log.WriteLine(LogLevel.Warning, 
                                  sprintf "%s:%s doesn't exist in target" ern eri')
                          | true -> a.Add(x.Key, new EntityReference(ern,eri))
                      | :? EntityCollection | _ -> a.Add(x)

                      a) (new AttributeCollection())
                    
              // Clear attributes and populate with new list
              e.Attributes.Clear()
              e.Attributes.AddRange(a'')

              // remove primary attribute
              match e.Attributes.Contains(em.PrimaryIdAttribute) with
              | true -> e.Attributes.Remove(em.PrimaryIdAttribute) |> ignore
              | false -> ()

              // remove second primary attribute on activitymimeattachments
              match e.LogicalName.Equals("activitymimeattachment") with
              | true -> 
                match e.Attributes.Contains("activityid") with
                | true -> e.Attributes.Remove("activityid") |> ignore
                | false -> ()
              | false -> ()

              // migrate legacy createdon (overriddencreatedon) except templates
              match e.Attributes.Contains(@"createdon") && 
                    e.LogicalName.Contains(@"template") |> not with
              | true ->
                let createdon = e.Attributes.["createdon"] :?> DateTime

                // Remove existing overriddencreatedon attribute
                match e.Attributes.Contains(@"overriddencreatedon") with
                | true -> e.Attributes.Remove(@"overriddencreatedon") |> ignore
                | false -> ()

                e.Attributes.Add(@"overriddencreatedon",createdon) |> ignore
              | false -> ()

              // add extra attributes
              attribs |> Map.iter(fun k v -> e.Attributes.Add(k,v))

              // create folder for imported
              let imported' = imported + en + @"\"

              ensureDirectory imported'
                
              let f' = imported' + Path.GetFileName(f)

              match File.Exists(f') with
              | false -> ()
              | true -> File.Delete(f')

              try
                match CrmDataInternal.Entities.existCrm p en ei (em.PrimaryIdAttribute |> Some) with
                | true ->   
                    CrmData.CRUD.update p e |> ignore
                      
                    File.Move(f,f')

                    log.WriteLine(LogLevel.Verbose, 
                        sprintf "%s:%s was updated" en ei')
                | false ->  
                    let guid = CrmData.CRUD.create p e (ParameterCollection())

                    File.Move(f,f')

                    log.WriteLine(LogLevel.Verbose, 
                        sprintf "%s:%s was created" en ei')
              with 
                  | _ as ex -> log.WriteLine(LogLevel.Warning, 
                                  sprintf "%s:%s %s" en ei' ex.Message)))

/// TODO: 
let import'' org ac location (log:ConsoleLogger) serialize 
  includeAttributes includeReferences referenceFilter additionalAttributes guidRemapping =
  let imported = location + @"..\imported\"

  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)

//    let i = Activator.CreateInstance<Entity>()
//    let t = Type.GetType (i.GetType().AssemblyQualifiedName)

  match Directory.Exists(location) with
  | false -> ()
  | true ->
    let ext = (@"." + Utility.unionToString serialize).ToLower()

    // Refactor mCache memoization (concurrent thread-safe)
    let dCache = 
      let dCacheHelper (proxy,entityname) =
        CrmData.Metadata.entity proxy entityname
      memoizeConcurrent' dCacheHelper

    // For each entity in location, populate EntityMetadata
    Directory.GetDirectories(location)
    |> Array.map(fun x ->
      let x' = Path.GetFileName(x)
      dCache(ServiceProxy.getOrganizationServiceProxy m tc, x'))
    |> ignore

    // Add memoization to avoid to many network queries (concurrent thread-safe)
    let dMem = 
      let dMemHelper (proxy,(entityname,entityid,primaryattribute)) =
        CrmDataInternal.Entities.existCrm proxy entityname entityid primaryattribute
      memoizeConcurrent' dMemHelper

    Directory.GetDirectories(location)
    |> Array.iter(fun location' ->
      Directory.EnumerateFiles(location', "*" + ext, SearchOption.AllDirectories)
      |> FSharpCoreExt.Seq.split (maxRequestInRequestCollection * (maxConcurrentMultipleRequest m.AuthenticationType))
      |> Seq.iter(fun xs ->
        xs
        |> Array.toSeq 
        |> FSharpCoreExt.Seq.split maxRequestInRequestCollection // (10 * (throttle m.AuthenticationType)) // Max of 1000 request in a request collection 
        |> Seq.toArray
        |> Array.Parallel.map(fun files ->
          let em = new ExecuteMultipleRequest()
          em.Settings <- new ExecuteMultipleSettings()
          em.Settings.ContinueOnError <- true
          em.Settings.ReturnResponses <- true
          em.Requests <- new OrganizationRequestCollection()

          files 
          |> Array.Parallel.map(fun f ->
            use p = ServiceProxy.getOrganizationServiceProxy m tc
            let e = SerializationHelper.deserializeFileToObject<Entity>
                      serialize f

            let en = e.LogicalName
            let em' = dCache(p, en)
            let ei = e.Id
            let ei' = ei.ToString()

            let a'' =
              e.Attributes
              |> Seq.fold(fun (a:AttributeCollection) x -> 
                match x.Value with
                | :? EntityReference as er -> 
                  match includeReferences with
                  | false -> ()
                  | true -> 
                    match Array.isEmpty referenceFilter, Array.contains er.LogicalName referenceFilter with
                    | false, false -> ()
                    | true, _ | false, true ->
                      let ern = er.LogicalName
                      let erm = dCache(p, ern)
                      let eri = dMapLookup guidRemapping ern er.Id ?| er.Id
                               
                      let eri' = eri.ToString()

                      let et = dMem (p, (ern,eri,erm.PrimaryIdAttribute |> Some))

                      match et with
                      | false -> 
                          log.WriteLine(LogLevel.Warning, 
                              sprintf "%s:%s doesn't exist in target" ern eri')
                      | true -> a.Add(x.Key, new EntityReference(ern,eri))
                | _ -> 
                  match includeAttributes with
                  | true -> a.Add(x.Key,x.Value)
                  | false ->                     
                    let enId = (e.LogicalName.ToLower() + "id") 
                    match x.Key with
                    | "id" | "logicalname"  -> a.Add(x.Key,x.Value)
                    | k when k = enId -> a.Add(x.Key,x.Value)
                    | _ -> ()
                a) (new AttributeCollection())

            // Clear attributes and populate with new list
            e.Attributes.Clear()
            e.Attributes.AddRange(a'')

            // remove primary attribute
            match e.Attributes.Contains(em'.PrimaryIdAttribute) with
            | true -> e.Attributes.Remove(em'.PrimaryIdAttribute) |> ignore
            | false -> ()

            // remove second primary attribute on activitymimeattachments
            match e.LogicalName.Equals("activitymimeattachment") with
            | true -> 
              match e.Attributes.Contains("activityid") with
              | true -> e.Attributes.Remove("activityid") |> ignore
              | false -> ()
            | false -> ()

            // Migrate legacy createdon (overriddencreatedon) 
            // Except templates ..?
            // Except duplicaterule and duplicaterulecondition as it will fail on import
            match e.Attributes.Contains(@"createdon") && 
                  e.LogicalName.Contains(@"template") |> not && 
                  e.LogicalName.Contains(@"duplicaterule") |> not with
            | true ->
              let createdon = e.Attributes.["createdon"] :?> DateTime
              e.Attributes.Add(@"overriddencreatedon",createdon) |> ignore
            | false -> ()

            // Cleanup of Duplicate Rule entities 
            match e.LogicalName.Equals(@"duplicaterule") || 
              e.LogicalName.Equals(@"duplicaterulecondition")with
            | false -> ()
            | true -> 
              // Remove statuscode and statecode for specific entities as they 
              // will fail on import
              e.Attributes.Remove("statecode") |> ignore
              e.Attributes.Remove("statuscode") |> ignore

              // duplicaterulecondition only: https://msdn.microsoft.com/en-us/library/gg334583.aspx
              match e.Attributes.ContainsKey "operatorcode" with
              | false ->
                ()
              | true ->
                match (e.Attributes.["operatorcode"] :?>OptionSetValue).Value with
                  | 0 ->
                    e.Attributes.Remove "operatorparam" |> ignore 
                  | _ ->
                    ()

            // add extra attributes
            additionalAttributes |> Map.iter(fun k v -> e.Attributes.Add(k,v))

            // check if create or update
            try
              match CrmDataInternal.Entities.existCrm p en ei (em'.PrimaryIdAttribute |> Some) with
                | true ->
                  CrmData.CRUD.updateReq e :> OrganizationRequest
                | false -> 
                  CrmData.CRUD.createReq e (ParameterCollection())
                    :> OrganizationRequest 
              |> Some            
            with 
              | _ as ex ->
                log.WriteLine(LogLevel.Warning, 
                  sprintf "%s:%s %s" en ei' ex.Message); None)
          |> Array.Parallel.choose(id)
          |> em.Requests.AddRange // OrganizationRequestCollection is not thread-safe
          //|> Array.iter(fun x -> em.Requests.Add(x)) // OrganizationRequestCollection is not thread-safe
          em,files)
      |> Array.Parallel.map(fun (em,files) ->
        try
          use p = ServiceProxy.getOrganizationServiceProxy m tc
          (p.Execute(em) :?> ExecuteMultipleResponse,files) |> Some
        with ex ->
          log.WriteLine(LogLevel.Warning, sprintf "%s" ex.Message); None)
      |> Array.Parallel.choose (id)
      |> Array.Parallel.iter(fun (em,files) -> 
        em.Responses
        |> Seq.toArray
        |> Array.Parallel.iter(fun kv ->
          let file = files.[kv.RequestIndex]
          let e = SerializationHelper.deserializeFileToObject<Entity>
                    serialize file

          let en = e.LogicalName
          let ei = e.Id
          let ei' = ei.ToString()

          match kv.Fault with
          | null ->
            // create folder for imported
            let imported' = imported + en + @"\"

            ensureDirectory imported'
                
            let file' = imported' + Path.GetFileName(file)

            match File.Exists(file') with
            | false -> ()
            | true -> File.Delete(file')

            File.Move(file,file')
                    
            log.WriteLine(LogLevel.Verbose,
              sprintf "%s:%s was created/updated" en ei')
          | fault ->
            log.WriteLine(LogLevel.Warning,
              sprintf "%s:%s %s" en ei' fault.Message)))))
                

/// TODO:
let associationImport' org ac location (log:ConsoleLogger) serialize guidRemapping =
  let aImported = location + @"..\associationImported\"

  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)

  let i = Activator.CreateInstance<Entity>()
  let t = Type.GetType (i.GetType().AssemblyQualifiedName)

  match Directory.Exists(location) with
  | false -> ()
  | true ->
    let ext = (@"." + Utility.unionToString serialize).ToLower()

    // Refactor mCache memoization (concurrent thread-safe)
    let dCache = 
      let dCacheHelper (proxy,entityname:string) =
        CrmData.Metadata.entity proxy entityname
      memoizeConcurrent' dCacheHelper

    let dCacheRel = 
      let dCacheRelHelper (proxy,entityname:string) =
        CrmData.Metadata.entityManyToManyRelationships proxy entityname
      memoizeConcurrent' dCacheRelHelper

    // For each entity in location, populate EntityMetadata
    Directory.GetDirectories(location)
    |> Array.map(fun x ->
        let x' = Path.GetFileName(x)
        dCache(ServiceProxy.getOrganizationServiceProxy m tc, x'),
        dCacheRel(ServiceProxy.getOrganizationServiceProxy m tc, x'))
    |> ignore

    Directory.EnumerateFiles(location, "*" + ext, SearchOption.AllDirectories)
    |> FSharpCoreExt.Seq.split (maxRequestInRequestCollection * (maxConcurrentMultipleRequest m.AuthenticationType))
    |> Seq.iter(fun xs ->
      xs
      |> Array.toSeq 
      |> FSharpCoreExt.Seq.split maxRequestInRequestCollection
      |> Seq.toArray
      |> Array.Parallel.map(fun files ->
        let em = new ExecuteMultipleRequest()
        em.Settings <- new ExecuteMultipleSettings()
        em.Settings.ContinueOnError <- true
        em.Settings.ReturnResponses <- true
        em.Requests <- new OrganizationRequestCollection()

        files 
        |> Array.Parallel.map(fun file ->
          use p = ServiceProxy.getOrganizationServiceProxy m tc
          let e = SerializationHelper.deserializeFileToObject<Entity>
                    serialize file

          let en = e.LogicalName
          let em = dCache(p, en)
          let sn = 
            let a' =
              dCacheRel(p, en)
              |> Array.filter (fun x -> x.IntersectEntityName = em.LogicalName)
            match a'.Length > 0 with
            | true -> a'.[0].SchemaName
            | false -> 
              failwith
                (sprintf "No valid SchemaName is defined for: %s" em.SchemaName)

          // remove primary attribute
          match e.Attributes.Contains(em.PrimaryIdAttribute) with
          | true -> e.Attributes.Remove(em.PrimaryIdAttribute) |> ignore
          | false -> ()

          let (x,y) =
            e.Attributes |> Seq.head, 
            e.Attributes |> Seq.skip 1 |> Seq.head

          let (x',y') =
            let matchToData (x:KeyValuePair<string,obj>) = 
              let x = KeyValuePair<string,Guid>(x.Key, (x.Value :?> Guid))
              match dMapLookup guidRemapping x.Key x.Value with
              | None -> x
              | Some v -> KeyValuePair<string,Guid>(x.Key, v)
            (matchToData x, matchToData y) 

          CrmDataInternal.Entities.createMany2ManyReq sn x' y' :> OrganizationRequest)
        |> em.Requests.AddRange //Array.iter(fun x -> em.Requests.Add(x)) // OrganizationRequestCollection is not thread-safe
        em,files)
      |> Array.Parallel.map(fun (em,files) ->
        try
          use p = ServiceProxy.getOrganizationServiceProxy m tc
          (p.Execute(em) :?> ExecuteMultipleResponse,files) |> Some
        with ex ->
          log.WriteLine(LogLevel.Warning, sprintf "%s" ex.Message); None)
      |> Array.Parallel.choose (id)
      |> Array.Parallel.iter(fun (em,files) -> 
        em.Responses
        |> Seq.toArray
        |> Array.Parallel.map(fun kv ->
          let file = files.[kv.RequestIndex]
          let id =
            match kv.Response with
            | null -> Guid.Empty
            | response ->
              match response.Results.Contains("id") with
              | false -> Guid.Empty
              | true -> response.Results.["id"] :?> Guid
          let error = 
            match kv.Fault with
            | null -> String.Empty
            | fault -> fault.Message
          file,id,error)
        |> Array.Parallel.iter(fun (file,id,error) ->
          // Deserialize again in order to retrieve correct logicalname
          let e = SerializationHelper.deserializeFileToObject<Entity>
                    serialize file

          let en = e.LogicalName
          let ei = e.Id
          let ei' = ei.ToString()

          match 
            not (id = Guid.Empty),
            error = "Cannot insert duplicate key." with
          | true, _ | _, true ->
            // create folder for associationImported
            let aImported' = aImported + en + @"\"

            ensureDirectory aImported'
                
            let f' = aImported' + Path.GetFileName(file)

            match File.Exists(f') with
            | false -> ()
            | true -> File.Delete(f')

            File.Move(file,f')

            log.WriteLine(LogLevel.Verbose,
              sprintf "%s:%s was created" en ei')
          | _,_ ->
            log.WriteLine(LogLevel.Warning,
              sprintf "%s:%s %s" en ei' error))))

// @deprecated
let reassignOwner' org ac location (log : ConsoleLogger) serialize 
    data = 
  let reassigned = location + @"..\reassigned\"
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  let i = Activator.CreateInstance<Entity>()
  let t = Type.GetType(i.GetType().AssemblyQualifiedName)
  match Directory.Exists(location) with
  | false -> ()
  | true -> 
    let ext = (@"." + Utility.unionToString serialize).ToLower()
    Directory.EnumerateFiles(location, "*" + ext, SearchOption.AllDirectories)
    |> Seq.toArray
    |> Array.Parallel.iter (fun f -> 
          use p = ServiceProxy.getOrganizationServiceProxy m tc
          let e = SerializationHelper.deserializeFileToObject<Entity> serialize f
          let en = e.LogicalName
          let ei = e.Id
          let ei' = ei.ToString()
          // create folder for reassigned
          let reassigned' = reassigned + en + @"\"
          ensureDirectory reassigned'
          let f' = reassigned' + Path.GetFileName(f)
          match File.Exists(f') with
          | false -> ()
          | true -> File.Delete(f')
          try 
            let owner = 
              match e.Attributes.Contains(@"ownerid") with
              | true -> 
                let oi = e.Attributes.["ownerid"] :?> EntityReference
                let oi' = dMapLookup data oi.LogicalName oi.Id ?| oi.Id
                oi' |> Some
              | false -> None
            match owner with
            | Some v -> 
              CrmDataInternal.Entities.assign p v e.LogicalName e.Id
              File.Move(f, f')
              log.WriteLine
                (LogLevel.Verbose, sprintf "%s:%s was reassigned" en ei')
            | None -> ()
          with ex -> 
            log.WriteLine(LogLevel.Warning, sprintf "%s:%s %s" en ei' ex.Message))


/// TODO: 
let reassignOwner'' org ac location (log : ConsoleLogger) 
    serialize data = 
  let reassigned = location + @"..\reassigned\"
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
//    let i = Activator.CreateInstance<Entity>()
//    let t = Type.GetType(i.GetType().AssemblyQualifiedName)
  // let t = typeof<Entity> // TODO: replace with this?
  match Directory.Exists(location) with
  | false -> ()
  | true -> 
    let ext = (@"." + Utility.unionToString serialize).ToLower()
        
    // Ensure that folders are created before populating files
    Directory.GetDirectories(location)
    |> Array.Parallel.iter (fun x -> 
        let logicalName = Path.GetFileName(x)
        let logicalName' = reassigned + logicalName + @"\"
        ensureDirectory logicalName')
    Directory.EnumerateFiles(location, "*" + ext, SearchOption.AllDirectories)
    |> FSharpCoreExt.Seq.split (maxRequestInRequestCollection * (maxConcurrentMultipleRequest m.AuthenticationType))
    |> Seq.iter (fun xs -> 
          xs
          |> Array.toSeq
          |> FSharpCoreExt.Seq.split maxRequestInRequestCollection
          |> Seq.toArray
          |> Array.Parallel.map (fun files -> 
              let em = new ExecuteMultipleRequest()
              em.Settings <- new ExecuteMultipleSettings()
              em.Settings.ContinueOnError <- true
              em.Settings.ReturnResponses <- true
              em.Requests <- new OrganizationRequestCollection()
              files
              |> Array.Parallel.map (fun file -> 
                    //use p = ServiceProxy.getOrganizationServiceProxy m tc
                    let e = 
                      SerializationHelper.deserializeFileToObject<Entity> 
                        serialize file
                    e, 
                    match e.Attributes.Contains(@"ownerid") with
                    | true -> 
                      let oi = e.Attributes.["ownerid"] :?> EntityReference
                      let oi' = dMapLookup data oi.LogicalName oi.Id ?| oi.Id
                      oi' |> Some
                    | false -> None)
              |> Array.Parallel.map (fun (e, owner) -> 
                    owner |> function 
                    | Some v -> (e, owner.Value) |> Some
                    | None -> None)
              |> Array.Parallel.choose (id)
              |> Array.Parallel.map 
                    (fun (e, owner) -> 
                    CrmDataInternal.Entities.assignReq owner e.LogicalName e.Id :> OrganizationRequest)
              |> em.Requests.AddRange //Array.iter (fun x -> em.Requests.Add(x)) // OrganizationRequestCollection is not thread-safe
              em, files)
          |> Array.Parallel.map (fun (em, files) -> 
              try 
                use p = ServiceProxy.getOrganizationServiceProxy m tc
                (p.Execute(em) :?> ExecuteMultipleResponse, files) |> Some
              with ex -> 
                log.WriteLine(LogLevel.Warning, sprintf "%s" ex.Message)
                None)
          |> Array.Parallel.choose (id)
          |> Array.Parallel.iter (fun (em, files) -> 
              em.Responses
              |> Seq.toArray
              |> Array.Parallel.iter 
                    (fun kv -> 
                    let file = files.[kv.RequestIndex]
                    let e = 
                      SerializationHelper.deserializeFileToObject<Entity> 
                        serialize file
                    let en = e.LogicalName
                    let ei = e.Id
                    let ei' = ei.ToString()
                    match kv.Fault with
                    | null -> 
                      // create folder for reassigned
                      let reassigned' = reassigned + en + @"\"
                      let file' = reassigned' + Path.GetFileName(file)
                      match File.Exists(file') with
                      | false -> ()
                      | true -> File.Delete(file')
                      File.Move(file, file')
                      log.WriteLine
                        (LogLevel.Verbose, sprintf "%s:%s was reassigned" en ei')
                    | fault -> 
                      log.WriteLine
                        (LogLevel.Warning, 
                        sprintf "%s:%s %s" en ei' fault.Message))))


// HACK:
let threadSafe = ref String.Empty
    
[ StructLayoutAttribute(LayoutKind.Sequential) ] |> ignore
    
type SYSTEMTIME = 
  struct
    val mutable wYear : int16
    val mutable wMonth : int16
    val mutable wDayOfWeek : int16
    val mutable wDay : int16
    val mutable wHour : int16
    val mutable wMinute : int16
    val mutable wSecond : int16
    val mutable wMilliseconds : int16
    new(wYear', wMonth', wDay', wHour', wMinute', wSecond', wDayOfWeek', 
        wMilliseconds') = 
      { wYear = wYear'
        wMonth = wMonth'
        wDay = wDay'
        wHour = wHour'
        wMinute = wMinute'
        wSecond = wSecond'
        wDayOfWeek = wDayOfWeek'
        wMilliseconds = wMilliseconds' }
  end
    
[<DllImport("kernel32.dll", SetLastError = true)>]
extern bool SetSystemTime(SYSTEMTIME& st)
    
let changeServerTime (dt : DateTime) = 
  System.Threading.Monitor.Enter threadSafe
  try 
    let mutable st = 
      SYSTEMTIME
        (int16 dt.Year, int16 dt.Month, int16 dt.Day, int16 dt.Hour, 
          int16 dt.Minute, int16 dt.Second, 0s, 0s)
    match SetSystemTime(&st) with
    | true -> ()
    | false -> 
      failwith 
        "Not enough rights to perform this operation. Execute as Administrator"
  finally
    System.Threading.Monitor.Exit threadSafe

/// @deprecated
let migrate' org ac location (log : ConsoleLogger) serialize map = 
  // TODO: Mimic what is done for imported ...
  let migrateded = location + @"..\migrateded\"
      
  let services = 
    ServiceController.GetServices()
    |> Array.map (fun x -> x.ServiceName)
    |> Set.ofArray
      
  let (wt, hvt) = 
    new ServiceController("W32Time"), 
    match Set.contains "vmictimesync" services with
    | false -> None
    | true -> Some(new ServiceController("vmictimesync"))
      
  try 
    wt.Stop()
    wt.WaitForStatus(ServiceControllerStatus.Stopped)
    match hvt with
    | None -> ()
    | Some v -> 
      v.Stop()
      v.WaitForStatus(ServiceControllerStatus.Stopped)
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc
    match Directory.Exists(location) with
    | false -> ()
    | true -> 
      let ext = (@"." + Utility.unionToString serialize).ToLower()
      let data = 
        Directory.EnumerateFiles
          (location, "*" + ext, SearchOption.AllDirectories)
      // HACK: created by
      data 
      |> Seq.iter (fun f -> 
            let e = 
              SerializationHelper.deserializeFileToObject<Entity> serialize f
            // HACK: callerID for created by
            let cb = e.Attributes.["createdby"] :?> EntityReference
            p.CallerId <- cb.Id
            // HACK: set server date for created on
            let co = e.Attributes.["createdon"] :?> DateTime
            changeServerTime co
            let en = e.LogicalName
            let ei = e.Id
            let ei' = ei.ToString()
               
            // check for related entities if they are in target
            let as' = 
              e.Attributes
              |> Seq.filter (fun a -> 
                  match a.Value.GetType() with
                  | t when t = typeof<EntityReference> -> 
                    let er = a.Value :?> EntityReference
                    let ern = er.LogicalName
                    let eri = er.Id
                    let eri' = eri.ToString()
                    // if not, add for later removal
                    let et = CrmDataInternal.Entities.existCrm p ern eri None
                    match et with
                    | false -> 
                      log.WriteLine
                        (LogLevel.Warning, 
                          sprintf "%s:%s doesn't exist in target" ern eri')
                    | true -> ()
                    not et
                  | _ -> false)
              |> Seq.toList
            // remove entity reference attributes that aren't in target
            as' |> Seq.iter (fun a -> e.Attributes.Remove(a.Key) |> ignore)
            // remove logicalName + id attribute
            match e.Attributes.Contains(en + @"id") with
            | true -> e.Attributes.Remove(en + @"id") |> ignore
            | false -> ()
            // add extra attributes
            map |> Map.iter (fun k v -> e.Attributes.Add(k, v))
            try 
              match CrmDataInternal.Entities.existCrm p en ei None with
              | true -> 
                CrmData.CRUD.update p e |> ignore
                log.WriteLine
                  (LogLevel.Verbose, sprintf "%s:%s was updated" en ei')
              | false -> 
                CrmData.CRUD.create p e (ParameterCollection()) |> ignore
                log.WriteLine
                  (LogLevel.Verbose, sprintf "%s:%s was created" en ei')
            with ex -> 
              log.WriteLine
                (LogLevel.Warning, sprintf "%s:%s %s" en ei' ex.Message))
      // HACK: modified by
      data 
      |> Seq.iter (fun f -> 
            let e = 
              SerializationHelper.deserializeFileToObject<Entity> serialize f
            // HACK: callerID for modified by
            let cb = e.Attributes.["modifiedby"] :?> EntityReference
            p.CallerId <- cb.Id
            // HACK: set server date for modified on
            let co = e.Attributes.["modifiedon"] :?> DateTime
            changeServerTime co
            let en = e.LogicalName
            let ei = e.Id
            let ei' = ei.ToString()
               
            // check for related entities if they are in target
            let as' = 
              e.Attributes
              |> Seq.filter (fun a -> 
                  match a.Value.GetType() with
                  | t when t = typeof<EntityReference> -> 
                    let er = a.Value :?> EntityReference
                    let ern = er.LogicalName
                    let eri = er.Id
                    let eri' = eri.ToString()
                    // if not, add for later removal
                    let et = CrmDataInternal.Entities.existCrm p ern eri None
                    match et with
                    | false -> 
                      log.WriteLine
                        (LogLevel.Warning, 
                          sprintf "%s:%s doesn't exist in target" ern eri')
                    | true -> ()
                    not et
                  | _ -> false)
              |> Seq.toList
            // remove entity reference attributes that aren't in target
            as' |> Seq.iter (fun a -> e.Attributes.Remove(a.Key) |> ignore)
            // remove logicalName + id attribute
            match e.Attributes.Contains(en + @"id") with
            | true -> e.Attributes.Remove(en + @"id") |> ignore
            | false -> ()
            // add extra attributes
            map |> Map.iter (fun k v -> e.Attributes.Add(k, v))
            try 
              match CrmDataInternal.Entities.existCrm p en ei None with
              | true -> 
                CrmData.CRUD.update p e |> ignore
                log.WriteLine
                  (LogLevel.Verbose, sprintf "%s:%s was updated" en ei')
              | false -> 
                CrmData.CRUD.create p e (ParameterCollection()) |> ignore
                log.WriteLine
                  (LogLevel.Verbose, sprintf "%s:%s was created" en ei')
            with ex -> 
              log.WriteLine
                (LogLevel.Warning, sprintf "%s:%s %s" en ei' ex.Message))
  finally
    wt.Start()
    wt.WaitForStatus(ServiceControllerStatus.Running)
    match hvt with
    | None -> ()
    | Some v -> 
      v.Start()
      v.WaitForStatus(ServiceControllerStatus.Running)


module DuplicateDetectionRules = 

  let makePublishDupRuleRequest guid =
        let req = PublishDuplicateRuleRequest()
        req.DuplicateRuleId <- guid
        req :> OrganizationRequest

  let retrieveDR proxy = 
    let f = FilterExpression()
    f.AddCondition(@"statecode", ConditionOperator.Equal, 0)
    f.AddCondition(@"statuscode", ConditionOperator.Equal, 0)
    let q = QueryExpression("duplicaterule")
    q.ColumnSet <- ColumnSet([|"duplicateruleid";"name"|])
    q.Criteria <- f
    CrmDataHelper.retrieveMultiple proxy q

  let publish org ac (dupRules:(string)[]) (log:ConsoleLogger) =

    match dupRules.Length with
    | 0 -> log.WriteLine(LogLevel.Info, "No Published Duplicate Detection Rules found in exported data")
    | _ -> 
      let m = ServiceManager.createOrgService org
      let tc = m.Authenticate(ac)
      use p = ServiceProxy.getOrganizationServiceProxy m tc
      
      retrieveDR p
      |> Seq.filter (fun x -> dupRules |> Array.contains (x.Attributes.["name"].ToString()))
      |> Array.ofSeq
      |> Array.map (fun x ->
        log.WriteLine(LogLevel.Verbose, sprintf "Publishing rule '%s' " (x.Attributes.["name"].ToString()))
        makePublishDupRuleRequest (Guid(x.Attributes.["duplicateruleid"].ToString())))
      |> CrmDataHelper.performAsBulk p
      |> Array.filter (fun x -> x.Fault = null)
      |> Array.length
      |> fun count -> log.WriteLine(LogLevel.Verbose, sprintf "Done publishing %d duplicate detection rules" count)
      