module internal DG.Daxif.Modules.Solution.SolutionHelper

open System
open System.IO
open System.Text
open Microsoft.Crm.Sdk
open Microsoft.Crm.Tools.SolutionPackager
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules.Serialization
open Microsoft.Xrm.Sdk.Client
open InternalUtility
open Utility
open CrmDataInternal
open CrmDataHelper


let publish proxyGen managed =  
  use p = proxyGen()
  if not managed then
    log.WriteLine(LogLevel.Verbose, @"Publishing solution")
    CrmDataHelper.publishAll p
    log.WriteLine
      (LogLevel.Verbose, @"The solution was successfully published")

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

let pluginSteps' proxyGen solution enable = 
  // Plugin: stateCode = 1 and statusCode = 2 (inactive), 
  //         stateCode = 0 and statusCode = 1 (active) 
  // Remark: statusCode = -1, will default the statuscode for the given statecode
  let state, status = 
    enable |> function 
    | false -> 1, (-1)
    | true -> 0, (-1)
      
  use p = proxyGen()

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let s = CrmDataInternal.Entities.retrieveSolutionId p solution
  CrmDataInternal.Entities.retrieveAllPluginProcessingSteps p s.Id
  |> Seq.toArray
  |> Array.Parallel.iter 
        (fun e -> 
        use p' = proxyGen()
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

let export' (proxyGen: unit -> OrganizationServiceProxy) solution location managed = 
  use p = proxyGen()
  //do p.Timeout <- new TimeSpan(0, 59, 0) // 59 minutes timeout
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

let executeImport (proxyGen: unit -> OrganizationServiceProxy) solution location managed =
  use p = proxyGen()
  do p.Timeout <- new TimeSpan(0, 59, 0) // 59 minutes timeout

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let zipFile = File.ReadAllBytes(location)

  log.WriteLine(LogLevel.Verbose, @"Solution file loaded successfully")

  let importJobId = Guid.NewGuid()
  let req = new Messages.ImportSolutionRequest()

  req.CustomizationFile <- zipFile
  req.ImportJobId <- importJobId
  req.ConvertToManaged <- managed
  req.OverwriteUnmanagedCustomizations <- true
  req.PublishWorkflows <- true

  log.WriteLine(LogLevel.Verbose, @"Proxy timeout set to 1 hour")

  let importHelper() = 
    async { 
      let asyncJobId = Import.startImportJob p req
      let! jobRes = Import.importLoop proxyGen solution importJobId asyncJobId
      Import.printJobResult asyncJobId jobRes
      return jobRes
    }
      
  let status = 
    importHelper()
    |> Async.Catch
    |> Async.RunSynchronously
    
  match status with
  | Choice1Of2 res -> 
    Import.printImportResult importJobId res
  | _ -> ()

  // Save the XML file
  log.WriteLine(LogLevel.Verbose, @"Fetching import job result")
  let location' = location.Replace(@".zip", "")
  let excel = location' + @"_" + Utility.timeStamp'() + @".xml"
  try
    Import.getXMLResult p importJobId
    |> Import.printAndSaveXMLResult excel
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

let exportWithExtendedSolution' (proxyGen: unit -> OrganizationServiceProxy) solution location managed = 
  export' (proxyGen: unit -> OrganizationServiceProxy) solution location managed
  let filename =
    let managed' =
      match managed with
      | true -> "_managed"
      | false -> ""
    sprintf "%s%s.zip" solution managed'
  log.WriteLine(LogLevel.Info, @"Exporting extended solution")
  ExtendedSolutionHelper.exportExtendedSolution (proxyGen: unit -> OrganizationServiceProxy) solution (location ++ filename)

let importWithExtendedSolution' proxyGen solution location managed = 
  executeImport proxyGen solution location managed |> ignore
  publish proxyGen managed
  log.WriteLine(LogLevel.Info, @"Importing extended solution")
  ExtendedSolutionHelper.unpackExtendedSolution location solution
  ||> ExtendedSolutionHelper.importExtendedSolution proxyGen

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

let updateXrmMockupMetadata' org location ap usr pwd domain exe log 
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
        "entities", (entities |> fun es -> String.Join(",", es))]
      
    let args = args @ extraArgs
    Utility.executeProcess (exe, args |> toArgStringDefault)
  postProcess (ccs()) log "DG XrmMockup"
    
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
