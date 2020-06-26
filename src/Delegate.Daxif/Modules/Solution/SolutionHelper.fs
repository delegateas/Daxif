module internal DG.Daxif.Modules.Solution.SolutionHelper

open System
open System.IO
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Common.CrmUtility
open Microsoft.Crm.Tools.SolutionPackager

let createPublisher' (env: Environment) name display prefix 
    (log : ConsoleLogger) = 
  let service = env.connect().GetService()
  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let pid = CrmDataInternal.Entities.createPublisher service name display prefix
  let msg = 
    @"Publisher was created successfully (Publisher ID: " + pid.ToString() 
    + @")"

  log.WriteLine(LogLevel.Verbose, msg)

let create' (env: Environment) name display pubPrefix (log : ConsoleLogger) = 
  let service = env.connect().GetService()

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let sid = CrmDataInternal.Entities.createSolution service name display pubPrefix
  let msg = 
    @"Solution was created successfully (Solution ID: " + sid.ToString() + @")"

  log.WriteLine(LogLevel.Verbose, msg)


let delete' (env: Environment) solution (log : ConsoleLogger) = 
  let service = env.connect().GetService()
  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let solutionId = CrmDataInternal.Entities.retrieveSolutionId service solution
  CrmData.CRUD.delete service "solution" solutionId |> ignore
  let msg = 
    @"Solution was deleted successfully (Solution ID: " + solutionId.ToString() + @")"

  log.WriteLine(LogLevel.Verbose, msg)

// TODO: Make compatible with CRM 2016 service pack 1. 
// 2016 service pack 1 cause problem due to patching function changing the way
// solution components are defined
let merge' (env: Environment) sourceSolution targetSolution (log : ConsoleLogger) =
    
  let getName (x:Entity) = x.Attributes.["uniquename"] :?> string

  let isManaged (solution: Entity) = solution.Attributes.["ismanaged"] :?> bool

  let getSolutionComponents proxy (solution:Entity) =
    CrmDataInternal.Entities.retrieveAllSolutionComponenets proxy solution.Id
    |> Seq.map(fun x -> 
      let uid = x.Attributes.["objectid"] :?> Guid
      let ct = x.Attributes.["componenttype"] :?> OptionSetValue
      (uid, ct.Value))
    |> Set.ofSeq

  let service = env.connect().GetService()
  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")    

  // fail if 2016 service pack 1 is used
  let v, _ = CrmDataInternal.Info.version service 
  match v.[0], v.[2] with
  | '8','2' -> failwith "Not supported in CRM 2016 Service Pack 1" 
  | _, _ ->

    // Retrieve solutions
    let source =
      CrmDataInternal.Entities.retrieveSolutionAllAttributes service sourceSolution
    let target =
      CrmDataInternal.Entities.retrieveSolutionAllAttributes service targetSolution
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
    | _,_ ->

      // Retrieve entities in target and source
      let guidSource = getSolutionComponents service source
      let guidTarget = getSolutionComponents service target

      // Creating a mapping from objectid to the entity logicname
      let uidToLogicNameMap = 
        CrmData.Metadata.allEntities service
        |> Seq.map(fun x -> x.MetadataId, x.LogicalName)
        |> Seq.filter(fun (mid,_) -> mid.HasValue)
        |> Seq.map(fun (mid,ln) -> mid.Value, ln)
        |> Map.ofSeq

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
          sprintf @"Adding %d component(s) from %s to the %s" x targetName sourceName)
        diff
        |> Seq.map(fun (uid,cType) ->
            
          log.WriteLine(LogLevel.Verbose,
            sprintf "Adding %s, %s to %s" 
              uidToLogicNameMap.[uid] (uid.ToString()) sourceName)

          let req = Messages.AddSolutionComponentRequest()
          req.ComponentId <- uid
          req.ComponentType <- cType
          req.SolutionUniqueName <- getName source
          req :> OrganizationRequest
          )
        |> Seq.toArray
        |> CrmDataInternal.CRUD.performAsBulkWithOutput service log

let pluginSteps' (env: Environment) solutionname enable (log : ConsoleLogger) = 
  // Plugin: stateCode = 1 and statusCode = 2 (inactive), 
  //         stateCode = 0 and statusCode = 1 (active) 
  // Remark: statusCode = -1, will default the statuscode for the given statecode
  let state, status = 
    enable |> function 
    | false -> 1, (-1)
    | true -> 0, (-1)
  
  let service = env.connect().GetService()
  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let solutionId = CrmDataInternal.Entities.retrieveSolutionId service solutionname
  CrmDataInternal.Entities.retrieveAllPluginProcessingSteps service solutionId
  |> Seq.toArray
  |> Array.Parallel.iter 
        (fun e -> 
        let en' = e.LogicalName
        let ei' = e.Id.ToString()
        try 
          CrmDataInternal.Entities.updateState service en' e.Id state status
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
    + solutionId.ToString() + @")"
  log.WriteLine(LogLevel.Verbose, msg)


let workflow' (env: Environment) solutionname enable (log : ConsoleLogger) = 
  // Workflow: stateCode = 0 and statusCode = 1 (inactive), 
  //           stateCode = 1 and statusCode = 2 (active)
  // Remark: statusCode = -1, will default the statuscode for the given statecode
  let state, status, retrievedStatus = 
    enable |> function 
    | false -> 0, (-1), 2
    | true -> 1, (-1), 1
      
  let service = env.connect().GetService()
  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let solutionId = CrmDataInternal.Entities.retrieveSolutionId service solutionname
  CrmDataInternal.Entities.retrieveWorkflowsOfStatus service solutionId retrievedStatus
  |> Seq.toArray
  |> fun w -> 
    match w.Length with
    | 0 -> log.WriteLine(LogLevel.Verbose, @"No workflows were updated")
    | _ -> 
      w 
      |> Array.Parallel.iter (fun e -> 
            let en' = e.LogicalName
            let ei' = e.Id.ToString()
            try 
              CrmDataInternal.Entities.updateState service en' e.Id state status
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
        + solutionId.ToString() + @")"
      log.WriteLine(LogLevel.Verbose, msg)

let exportWithExtendedSolution (env: Environment) solution location managed = 
  let service = env.connect().GetService()
  let solutionPath = Export.execute service solution location managed log
  Extend.export service solution solutionPath
  solutionPath

let importWithExtendedSolution reassignWorkflows (env: Environment) solution location managed = 
  let service = env.connect().GetService()
  Extend.preImport service solution location
  Import.execute service solution location managed
  |> fun jobInfo -> jobInfo.result
  |> function
    | Some (Domain.ImportResult.Success) ->
      Import.publish service managed
      Extend.postImport service solution location reassignWorkflows
    | _ -> ()

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


let updateServiceContext' (env: Environment) location exe lcid (log:ConsoleLogger) =
  let lcid : int option = lcid
  let lcid' = 
    match lcid with
    | Some v -> string v
    | None -> System.String.Empty
  
  let orgString = env.url.ToString()
  let csu() =
    let args = 
      [ "/metadataproviderservice:\"DG.MetadataProvider.IfdMetadataProviderService, Delegate.MetadataProvider\""
        sprintf "/url:\"%s\"" orgString
        //sprintf "/username:\"%s\"" usr
        //sprintf "/password:\"%s\"" pwd
        //sprintf "/domain:\"%s\"" domain
        "/language:cs"
        "/namespace:DG.XrmFramework.BusinessDomain.ServiceContext"
        "/serviceContextName:Xrm"
        sprintf "/out:\"%s\Xrm.cs\"" location
      ] |> String.concat " "
    Utility.executeProcess(exe,args)

  let csu'() =
    let args = ""
      //(sprintf "/metadataproviderservice:\"DG.MetadataProvider.IfdMetadataProviderService, Delegate.MetadataProvider\" \
      //          /codewriterfilter:\"Microsoft.Crm.Sdk.Samples.FilteringService, Delegate.GeneratePicklistEnums\" \
      //          /codecustomization:\"Microsoft.Crm.Sdk.Samples.CodeCustomizationService, Delegate.GeneratePicklistEnums\" \
      //          /namingservice:\"Microsoft.Crm.Sdk.Samples.NamingService%s, Delegate.GeneratePicklistEnums\" \
      //          /url:\"%s\" \
      //          /username:\"%s\" \
      //          /password:\"%s\" \
      //          /domain:\"%s\" \
      //          /language:\"cs\" \
      //          /namespace:\"DG.XrmFramework.BusinessDomain.ServiceContext.OptionSets\" \
      //          /serviceContextName:\"XrmOptionSets\" \
      //          /out:\"%s\\XrmOptionSets.cs\"" lcid' (env.url.ToString()) usr pwd domain location)
    Utility.executeProcess(exe,args)

  postProcess (csu()) log "MS CrmSvcUtil SDK"
  postProcess (csu'()) log "MS CrmSvcUtil SDK (Option Sets)"


let addIfSome (key: string) (v: string option) (list: (string * string) list) =
  match v with
  | None -> list
  | Some v' -> (key,v') :: list

let getOptionalUsrPwdDmn (env: Environment) =
  match env.method with
  | ClientSecret
  | ConnectionString -> None,None,None
  | Proxy
  | OAuth ->
    let usr, pwd, dmn = env.getCreds()
    Some usr, Some pwd, Some dmn

let updateCustomServiceContext' (env: Environment) location exe log 
    (solutions : string list) (entities : string list) extraArgs = 
  let ccs() = 
    let baseArgs = 
      [ "ap", env.ap.ToString()
        "out", location
        "method", env.method.ToString()
        "solutions", (solutions |> fun ss -> String.Join(",", ss))
        "entities", (entities |> fun es -> String.Join(",", es))
        "servicecontextname", "Xrm"
        "namespace", "DG.XrmFramework.BusinessDomain.ServiceContext" ]
    
    let usr,pwd,dmn = getOptionalUsrPwdDmn env
    let optionalArgs =
      baseArgs
      |>(
        addIfSome "url" (env.url ?|> fun x -> x.ToString()) >>
        addIfSome "username" usr >>
        addIfSome "password" pwd >>
        addIfSome "domain" dmn >>
        addIfSome "mfaAppId" env.clientId >>
        addIfSome "mfaReturnUrl" env.returnUrl >>
        addIfSome "mfaClientSecret" env.clientSecret >>
        addIfSome "connectionString" env.connectionString
      )
     
    let args = optionalArgs @ extraArgs
    Utility.executeProcess (exe, args |> toArgStringDefault)
  postProcess (ccs()) log "DG XrmContext"

let updateXrmMockupMetadata' (env: Environment) location exe log 
    (solutions : string list) (entities : string list) extraArgs = 
  let ccs() = 
    let baseArgs = 
      [ "ap", env.ap.ToString()
        "method", env.method.ToString()
        "out", location
        "solutions", (solutions |> fun ss -> String.Join(",", ss))
        "entities", (entities |> fun es -> String.Join(",", es)) ]

    let usr,pwd,dmn = getOptionalUsrPwdDmn env
    let optionalArgs =
      baseArgs
      |>(
        addIfSome "url" (env.url ?|> fun x -> x.ToString()) >>
        addIfSome "username" usr >>
        addIfSome "password" pwd >>
        addIfSome "domain" dmn >>
        addIfSome "mfaAppId" env.clientId >>
        addIfSome "mfaReturnUrl" env.returnUrl >>
        addIfSome "mfaClientSecret" env.clientSecret >>
        addIfSome "connectionString" env.connectionString

      )
      
    let finalArgs = optionalArgs @ extraArgs
    Utility.executeProcess (exe, finalArgs |> toArgStringDefault)
  postProcess (ccs()) log "DG XrmMockup"
    
let updateTypeScriptContext' (env: Environment) location exe log 
    (solutions : string list) (entities : string list) extraArgs = 
  let dts() = 
    let baseArgs = 
      [ "ap", env.ap.ToString()
        "method", env.method.ToString()
        "out", location
        "solutions", (solutions |> fun ss -> String.Join(",", ss))
        "entities", (entities |> fun es -> String.Join(",", es)) ]

    let usr,pwd,dmn = getOptionalUsrPwdDmn env
    let optionalArgs =
      baseArgs
      |>(
        addIfSome "url" (env.url ?|> fun x -> x.ToString()) >>
        addIfSome "username" usr >>
        addIfSome "password" pwd >>
        addIfSome "domain" dmn >>
        addIfSome "mfaAppId" env.clientId >>
        addIfSome "mfaReturnUrl" env.returnUrl >>
        addIfSome "mfaClientSecret" env.clientSecret >>
        addIfSome "connectionString" env.connectionString
      )
      
    let finalArgs = optionalArgs @ extraArgs
    Utility.executeProcess (exe, finalArgs |> toArgStringDefault)
  postProcess (dts()) log "Delegate XrmDefinitelyTyped"

let count' (env: Environment) solutionName = 
  let service = env.connect().GetService()
  let solutionId = CrmDataInternal.Entities.retrieveSolutionId service solutionName
  CrmDataInternal.Entities.countEntities service solutionId
