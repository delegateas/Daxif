module internal DG.Daxif.Modules.Solution.SolutionHelper

open System
open System.IO
open System.Text
open Microsoft.Crm.Sdk
open Microsoft.Crm.Tools.SolutionPackager
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.CrmDataInternal
open DG.Daxif.Modules.Serialization

let createPublisher' org ac name display prefix 
    (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let pid = CrmDataInternal.Entities.createPublisher p name display prefix
  let msg = 
    @"Publisher was created successfully (Publisher ID: " + pid.ToString() 
    + @")"

  log.WriteLine(LogLevel.Verbose, msg)

let create' org ac name display pubPrefix (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let sid = CrmDataInternal.Entities.createSolution p name display pubPrefix
  let msg = 
    @"Solution was created successfully (Solution ID: " + sid.ToString() + @")"

  log.WriteLine(LogLevel.Verbose, msg)


let delete' org ac solution (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let s = CrmDataInternal.Entities.retrieveSolutionId p solution
  CrmData.CRUD.delete p s.LogicalName s.Id |> ignore
  let msg = 
    @"Solution was deleted successfully (Solution ID: " + s.Id.ToString() + @")"

  log.WriteLine(LogLevel.Verbose, msg)

// TODO: Make compatible with CRM 2016 service pack 1. 
// 2016 service pack 1 cause problem due to patching function changing the way
// solution components are defined
let merge' org ac sourceSolution targetSolution (log : ConsoleLogger) =
    
  let getName (x:Entity) = x.Attributes.["uniquename"] :?> string

  let isManaged (solution: Entity) = solution.Attributes.["ismanaged"] :?> bool

  let getSolutionComponents proxy (solution:Entity) fullEntities =
    CrmDataInternal.Entities.retrieveAllSolutionComponenets proxy solution.Id fullEntities
    |> Seq.map(fun x -> 
      let uid = x.Attributes.["objectid"] :?> Guid
      let ct = x.Attributes.["componenttype"] :?> OptionSetValue
      (uid, ct.Value))
    |> Set.ofSeq

  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc

  log.Verbose @"Service Manager instantiated"
  log.Verbose @"Service Proxy instantiated"

  // Retrieve solutions
  let source =
    CrmDataInternal.Entities.retrieveSolutionAllAttributes p sourceSolution
  let target =
    CrmDataInternal.Entities.retrieveSolutionAllAttributes p targetSolution
  let sourceName = getName source
  let targetName = getName target

  // Ensure both solution are unmanaged
  match isManaged source, isManaged target with
  | true,_ ->
    failwith (sprintf "Unable to merge %s as it is a managed solution"
      sourceName)
  | _,true ->
    failwith (sprintf "Unable to merge %s as it is a managed solution"
      targetName)
  | _,_ -> ()

  // Prior to CRM 2016 service pack 1, only full entities could be included in solutions
  let fullEntities =
    match CrmDataInternal.Info.version p with
    | _, CrmReleases.CRM2011
    | _, CrmReleases.CRM2013
    | _, CrmReleases.CRM2015 -> true
    | x, CrmReleases.CRM2016 when x.[2] < '2' -> true
    | _ -> false

  // Retrieve entities in target and source
  let guidSource = getSolutionComponents p source fullEntities
  let guidTarget = getSolutionComponents p target fullEntities

  // Create a mapping from objectid to entity logicalname
  let uidToLogicNameMap = 
    match fullEntities with
    | true  -> 
      CrmData.Metadata.allEntities p
      |> Seq.map(fun x -> x.MetadataId, x.LogicalName)
      |> Seq.filter(fun (mid,_) -> mid.HasValue)
      |> Seq.map(fun (mid,ln) -> mid.Value, ln)
      |> Map.ofSeq
    | false -> Map.empty

//  uidToLogicNameMap
//  |> Map.iter (fun key value -> log.Verbose "%s - %s" (key.ToString()) value)

  // Find entities in target which does not exist in source
  let diff = guidTarget - guidSource

  // Make new solutioncomponent for source solution with entities in diff
  match diff.Count with
  | 0 -> 
    log.WriteLine(LogLevel.Info,
      sprintf @"Nothing to merge, components in %s already exist in %s"
        targetName sourceName)
  | x -> 
    log.WriteLine(LogLevel.Verbose,
      sprintf @"Adding %d component(s) from %s to %s" x targetName sourceName)
    diff
    |> Seq.map(fun (uid, cType) ->

      match Map.tryFind uid uidToLogicNameMap with
      | Some s -> log.Verbose "Adding %s (%s)" s (uid.ToString())
      | None -> log.Verbose "Adding 'ComponentType %i' (%s)" cType (uid.ToString())

      let req = Messages.AddSolutionComponentRequest()
      req.ComponentId <- uid
      req.ComponentType <- cType
      req.SolutionUniqueName <- getName source
      req :> OrganizationRequest
      )
    |> Seq.toArray
    |> CrmDataInternal.CRUD.performAsBulkWithOutput p log

let pluginSteps' org ac solution enable (log : ConsoleLogger) = 
  // Plugin: stateCode = 1 and statusCode = 2 (inactive), 
  //         stateCode = 0 and statusCode = 1 (active) 
  // Remark: statusCode = -1, will default the statuscode for the given statecode
  let state, status = 
    enable |> function 
    | false -> 1, (-1)
    | true -> 0, (-1)
      
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let s = CrmDataInternal.Entities.retrieveSolutionId p solution
  CrmDataInternal.Entities.retrieveAllPluginProcessingSteps p s.Id
  |> Seq.toArray
  |> Array.Parallel.iter 
        (fun e -> 
        use p' = ServiceProxy.getOrganizationServiceProxy m tc
        let en' = e.LogicalName
        let ei' = e.Id.ToString()
        try 
          CrmDataInternal.Entities.updateState p' en' e.Id state status
          log.WriteLine
            (LogLevel.Verbose, sprintf "%s:%s state was updated" en' ei')
        with ex -> 
          log.WriteLine(LogLevel.Warning, sprintf "%s:%s %s" en' ei' ex.Message))
  let msg' = 
    enable |> function 
    | true -> "enabled"
    | false -> "disabled"
      
  let msg = 
    @"The solution plugins were successfully " + msg' + @"(Solution ID: " 
    + s.Id.ToString() + @")"
  log.WriteLine(LogLevel.Verbose, msg)


let workflow' org ac solution enable (log : ConsoleLogger) = 
  // Workflow: stateCode = 0 and statusCode = 1 (inactive), 
  //           stateCode = 1 and statusCode = 2 (active)
  // Remark: statusCode = -1, will default the statuscode for the given statecode
  let state, status, retrievedStatus = 
    enable |> function 
    | false -> 0, (-1), 2
    | true -> 1, (-1), 1
      
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let s = CrmDataInternal.Entities.retrieveSolutionId p solution
  CrmDataInternal.Entities.retrieveWorkflowsOfStatus p s.Id retrievedStatus
  |> Seq.toArray
  |> fun w -> 
    match w.Length with
    | 0 -> log.WriteLine(LogLevel.Verbose, @"No workflows were updated")
    | _ -> 
      w 
      |> Array.Parallel.iter (fun e -> 
            use p' = ServiceProxy.getOrganizationServiceProxy m tc
            let en' = e.LogicalName
            let ei' = e.Id.ToString()
            try 
              CrmDataInternal.Entities.updateState p' en' e.Id state status
              log.WriteLine
                (LogLevel.Verbose, sprintf "%s:%s state was updated" en' ei')
            with ex -> 
              log.WriteLine
                (LogLevel.Warning, sprintf "%s:%s %s" en' ei' ex.Message))
      let msg' = 
        enable |> function 
        | true -> "enabled"
        | false -> "disabled"
          
      let msg = 
        @"The solution workflows were successfully " + msg' + @" (Solution ID: " 
        + s.Id.ToString() + @")"
      log.WriteLine(LogLevel.Verbose, msg)


let export' org ac solution location managed (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  do p.Timeout <- new TimeSpan(0, 59, 0) // 59 minutes timeout
  let req = new Messages.ExportSolutionRequest()

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  req.Managed <- managed
  req.SolutionName <- solution

  log.WriteLine(LogLevel.Verbose, @"Proxy timeout set to 1 hour")
  log.WriteLine(LogLevel.Verbose, @"Export solution")

  let resp = p.Execute(req) :?> Messages.ExportSolutionResponse

  log.WriteLine(LogLevel.Verbose, @"Solution was exported successfully")

  let zipFile = resp.ExportSolutionFile
  let filename =
    let managed' =
      match managed with
      | true -> "_managed"
      | false -> ""
    sprintf "%s%s.zip" solution managed'

  File.WriteAllBytes(location ++ filename, zipFile)

  log.WriteLine(LogLevel.Verbose, @"Solution saved to local disk")

let import' org ac solution location managed (log : ConsoleLogger) = 
  
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  do p.Timeout <- new TimeSpan(0, 59, 0) // 59 minutes timeout

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let zipFile = File.ReadAllBytes(location)

  log.WriteLine(LogLevel.Verbose, @"Solution file loaded successfully")

  let jobId = Guid.NewGuid()
  let req = new Messages.ImportSolutionRequest()

  req.CustomizationFile <- zipFile
  req.ImportJobId <- jobId
  req.ConvertToManaged <- managed
  req.OverwriteUnmanagedCustomizations <- true
  req.PublishWorkflows <- true

  log.WriteLine(LogLevel.Verbose, @"Proxy timeout set to 1 hour")

  let rec importHelper' exists completed progress aJobId = 
    async { 
      use p' = ServiceProxy.getOrganizationServiceProxy m tc
      match exists with
      | false -> 

        // Check to ensure that the async job is started at all
        match aJobId with
        | None -> ()
        | Some id ->
          match Info.retrieveAsyncJobState p' id with
          | AsyncJobState.Failed | AsyncJobState.Canceled ->
            log.WriteLine(LogLevel.Verbose, "Asynchronous import job failed")
            let systemJob = CrmData.CRUD.retrieve p' "asyncoperation" id
            let msg = 
              match systemJob.Attributes.ContainsKey "message" with
              | true -> systemJob.Attributes.["message"] :?> string
              | false -> "No failure message"
            msg
            |> sprintf "Failed with message: %s"
            |> failwith 
          | _ -> ()

        let exists' =
          CrmDataInternal.Entities.existCrm p' @"importjob" jobId None
        do! importHelper' exists' completed progress aJobId
      | true -> 
        match completed with
        | true -> ()
        | false -> 
          do! Async.Sleep(10 * 1000) // 10 seconds
          let (pct, completed') = 
            use p' = ServiceProxy.getOrganizationServiceProxy m tc
            try 
                
              let j = CrmDataInternal.Entities.retrieveImportJobWithXML p' jobId
              let progress' = j.Attributes.["progress"] :?> double

              // Check if the job is completed
              match aJobId with
              | None ->
                (progress', j.Attributes.Contains("completedon"))
              | Some id ->
                try
                  match Info.retrieveAsyncJobState p' id with
                  | AsyncJobState.Succeeded 
                  | AsyncJobState.Failed 
                  | AsyncJobState.Canceled ->
                    (progress', true)
                  | _ -> (progress', false)
                with _ -> (progress', false)
            with _ -> (progress, false)
          match completed' with
          | false -> 
            sprintf @"Import solution: %s (%i%%)" solution (pct |> int)
            |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)
          | true -> 
            use p' = ServiceProxy.getOrganizationServiceProxy m tc

            let status = 
              try 
                let j = CrmDataInternal.Entities.retrieveImportJobWithXML p' jobId
                let progress' = j.Attributes.["progress"] :?> double

                // Get the result of the import job
                match aJobId with
                | None -> 
                  log.WriteLine(LogLevel.Verbose,@"Import job completed")
                  let data = j.Attributes.["data"] :?> string
                  let success = not (data.Contains("<result result=\"failure\""))

                  (progress' = 100.) || success
                | Some id -> 
                  log.WriteLine(LogLevel.Verbose,@"Asynchronous import job completed")
                  let success = 
                    match Info.retrieveAsyncJobState p' id with
                    | AsyncJobState.Succeeded -> true
                    | _ -> false

                  (progress' = 100.) || success                  
              with _ -> false
            match status with
            | true -> 
              sprintf  @"Solution import succeeded (ImportJob ID: %A)" jobId 
              |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)
            | false ->
              let msg =
                match aJobId with
                | None -> 
                  (sprintf @"Solution import failed (ImportJob ID: %A)" jobId)
                | Some(id) ->
                  let systemJob = CrmData.CRUD.retrieve p' "asyncoperation" id
                  let msg = 
                    match systemJob.Attributes.ContainsKey "message" with
                    | true -> systemJob.Attributes.["message"] :?> string
                    | false -> "No failure message"
                  (jobId, msg)
                  ||> sprintf "Solution import failed (ImportJob ID: %A) with message %s"
              failwith msg
            match managed with
            | true -> ()
            | false -> 
              log.WriteLine(LogLevel.Verbose, @"Publishing solution")
              CrmDataHelper.publishAll p'
              log.WriteLine
                (LogLevel.Verbose, @"The solution was successfully published")
            return ()
          do! importHelper' exists completed' pct aJobId
    }
      
  let importHelperAsync() = 
    // Added helper function in order to not having to look for the 
    // Messages.ExecuteAsyncRequest Type for MS CRM 2011 (legacy)
    let areq = new Messages.ExecuteAsyncRequest()
    areq.Request <- req
    p.Execute(areq) :?> Messages.ExecuteAsyncResponse 
    |> fun r -> r.AsyncJobId
      
  let importHelper() = 
    async { 
      let aJobId = 
        match CrmDataInternal.Info.version p with
        | (_, CrmReleases.CRM2011) -> 
          p.Execute(req) :?> Messages.ImportSolutionResponse |> ignore
          log.WriteLine(LogLevel.Verbose,@"Import job Started")
          None
        | (_, _) -> 
          log.WriteLine(LogLevel.Verbose,@"Asynchronous import job started")
          Some (importHelperAsync())
      log.WriteLine(LogLevel.Verbose, @"Import solution: " + solution + @" (0%)")

      let! progress = importHelper' false false 0. aJobId
      progress
    }
      
  let status = 
    importHelper()
    |> Async.Catch
    |> Async.RunSynchronously
    
      
  // Save the XML file
  log.WriteLine(LogLevel.Verbose, @"Fetching import job result")
  let location' = location.Replace(@".zip", "")
  let excel = location' + @"_" + Utility.timeStamp'() + @".xml"
  try  
    let req' = new Messages.RetrieveFormattedImportJobResultsRequest()
    req'.ImportJobId <- jobId
    let resp' = 
      p.Execute(req') :?> Messages.RetrieveFormattedImportJobResultsResponse
    let xml = resp'.FormattedResults
    let bytes = Encoding.UTF8.GetBytes(xml)
    let bytes' = SerializationHelper.xmlPrettyPrinterHelper' bytes
    let xml' = "<?xml version=\"1.0\"?>\n" + (Encoding.UTF8.GetString(bytes'))
    File.WriteAllText(excel, xml')
    log.WriteLine(LogLevel.Verbose, @"Import solution results saved to: " + excel)
  with 
  | ex -> 
    match status with
    | Choice2Of2 exn -> 
      log.WriteLine(LogLevel.Error, exn.Message)
      raise ex
    | _ -> raise ex
    
  // Rethrow exception in case of failure
  match status with
  | Choice2Of2 exn -> raise exn
  | _ -> excel

let exportWithDGSolution' org ac ac' solution location managed (log : ConsoleLogger) = 
  export' org ac solution location managed log
  let filename =
    let managed' =
      match managed with
      | true -> "_managed"
      | false -> ""
    sprintf "%s%s.zip" solution managed'
  log.WriteLine(LogLevel.Info, @"Exporting DGSolution")
  DGSolutionHelper.exportDGSolution org ac' solution (location ++ filename) log

let importWithDGSolution' org ac ac' solution location managed (log : ConsoleLogger) = 
  import' org ac solution location managed log |> ignore
  log.WriteLine(LogLevel.Info, @"Importing DGSolution")
  DGSolutionHelper.importDGSolution org ac' solution location

//TODO:
let extract' location (customizations : string) (map : string) project 
    (log : ConsoleLogger) (logl : LogLevel) = 
  let logl' = Enum.GetName(typeof<LogLevel>, logl)
  let pa = new PackagerArguments()
  log.WriteLine(LogLevel.Info, "Start output from SolutionPackager")
  // Use parser to ensure proper initialization of arguments
  Parser.ParseArgumentsWithUsage(
    [|  
      "/action:Extract"
      "/packagetype:Both"
      sprintf @"/zipfile:%s" location
      sprintf @"/folder:%s" customizations
      sprintf @"/map:%s" map
      sprintf @"/errorlevel:%s" logl'
      "/allowDelete:Yes"
      "/clobber"
    |], pa)
  |> ignore
  try 
    let sp = new SolutionPackager(pa)
    sp.Run()
  with ex -> log.WriteLine(LogLevel.Error, sprintf "%s" ex.Message)
  log.WriteLine(LogLevel.Info, "End output from SolutionPackager")
  InternalUtility.touch project
  ()
      
//TODO:
let pack' location customizations map managed (log : ConsoleLogger) (logl : LogLevel) = 
  let logl' = Enum.GetName(typeof<LogLevel>, logl)
  let pa = new PackagerArguments()
  let managed' = match managed with | true -> "Managed" | false -> "Unmanaged"
  log.WriteLine(LogLevel.Info, "Start output from SolutionPackager")
  // Use parser to ensure proper initialization of arguments
  Parser.ParseArgumentsWithUsage(
    [| "/action:Pack";
        sprintf @"/packagetype:%s" managed'; 
        sprintf @"/zipfile:%s" location;
        sprintf @"/folder:%s" customizations;
        sprintf @"/map:%s" map;
        sprintf @"/errorlevel:%s" logl'; |], pa)
  |> ignore
  try 
    let sp = SolutionPackager(pa)
    sp.Run()
  with ex -> log.WriteLine(LogLevel.Error, ex.Message)
  log.WriteLine(LogLevel.Info, "End output from SolutionPackager")


let updateServiceContext' (org:Uri) location ap usr pwd domain exe lcid (log:ConsoleLogger) =
  let lcid : int option = lcid
  let lcid' = 
    match lcid with
    | Some v -> string v
    | None -> System.String.Empty
  
  let orgString = org.ToString()
  let csu() =
    let args = 
      [ "/metadataproviderservice:\"DG.MetadataProvider.IfdMetadataProviderService, Delegate.MetadataProvider\""
        sprintf "/url:\"%s\"" orgString
        sprintf "/username:\"%s\"" usr
        sprintf "/password:\"%s\"" pwd
        sprintf "/domain:\"%s\"" domain
        "/language:cs"
        "/namespace:DG.XrmFramework.BusinessDomain.ServiceContext"
        "/serviceContextName:Xrm"
        sprintf "/out:\"%s\Xrm.cs\"" location
      ] |> String.concat " "
    Utility.executeProcess(exe,args)

  let csu'() =
    let args =
      (sprintf "/metadataproviderservice:\"DG.MetadataProvider.IfdMetadataProviderService, Delegate.MetadataProvider\" \
                /codewriterfilter:\"Microsoft.Crm.Sdk.Samples.FilteringService, Delegate.GeneratePicklistEnums\" \
                /codecustomization:\"Microsoft.Crm.Sdk.Samples.CodeCustomizationService, Delegate.GeneratePicklistEnums\" \
                /namingservice:\"Microsoft.Crm.Sdk.Samples.NamingService%s, Delegate.GeneratePicklistEnums\" \
                /url:\"%s\" \
                /username:\"%s\" \
                /password:\"%s\" \
                /domain:\"%s\" \
                /language:\"cs\" \
                /namespace:\"DG.XrmFramework.BusinessDomain.ServiceContext.OptionSets\" \
                /serviceContextName:\"XrmOptionSets\" \
                /out:\"%s\\XrmOptionSets.cs\"" lcid' (org.ToString()) usr pwd domain location)
    Utility.executeProcess(exe,args)

  postProcess (csu()) log "MS CrmSvcUtil SDK"
  postProcess (csu'()) log "MS CrmSvcUtil SDK (Option Sets)"


let updateCustomServiceContext' org location ap usr pwd domain exe log 
    (solutions : string list) (entities : string list) extraArgs = 
  let ccs() = 
    let args = 
      [ "url", org.ToString()
        "username", usr
        "password", pwd
        "domain", domain
        "ap", ap.ToString()
        "out", location
        "solutions", (solutions |> fun ss -> String.Join(",", ss))
        "entities", (entities |> fun es -> String.Join(",", es))
        "servicecontextname", "Xrm"
        "namespace", "DG.XrmFramework.BusinessDomain.ServiceContext" ]
      
    let args = args @ extraArgs
    Utility.executeProcess (exe, args |> toArgStringDefault)
  postProcess (ccs()) log "DG XrmContext"
    
let updateTypeScriptContext' org location ap usr pwd domain exe log 
    (solutions : string list) (entities : string list) extraArgs = 
  let dts() = 
    let args = 
      [ "url", org.ToString()
        "username", usr
        "password", pwd
        "domain", domain
        "ap", ap.ToString()
        "out", location
        "solutions", (solutions |> fun ss -> String.Join(",", ss))
        "entities", (entities |> fun es -> String.Join(",", es)) ]
      
    let args = args @ extraArgs
    Utility.executeProcess (exe, args |> toArgStringDefault)
  postProcess (dts()) log "Delegate XrmDefinitelyTyped"

let count' org solutionName ac = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  let solution = CrmDataInternal.Entities.retrieveSolutionId p solutionName
  CrmDataInternal.Entities.countEntities p solution.Id
