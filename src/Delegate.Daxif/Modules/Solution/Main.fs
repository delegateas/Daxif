﻿module DG.Daxif.Modules.Solution.Main

open System
open System.IO
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let createPublisher (env: Environment) name display prefix log = 
  let log = ConsoleLogger log
  logVersion log
  log.Info @"Create publisher: %s" display
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Name: %s" name
  log.Verbose @"Display name: %s" display
  log.Verbose @"Prefix: %s" prefix
  env.logAuthentication log
  SolutionHelper.createPublisher' env name display prefix log
  log.Info @"The publisher was created successfully."
  
let create (env: Environment) name display pubPrefix log = 
  let log = ConsoleLogger log
  logVersion log

  log.Info @"Create solution: %s" display
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Name: %s" name
  log.Verbose @"Display name: %s" display
  log.Verbose @"Publisher prefix: %s" pubPrefix
  env.logAuthentication log
  SolutionHelper.create' env name display pubPrefix log
  log.Info @"The solution was created successfully."
  
let delete (env: Environment) solution log = 
  let log = ConsoleLogger log
  logVersion log

  log.Info @"Delete solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  env.logAuthentication log
  SolutionHelper.delete' env solution log
  log.Info @"The deleted was created successfully."

let merge (env: Environment) sourceSolution targetSolution log =
  let log = ConsoleLogger log
  logVersion log

  log.Info @"Merging %s solution into %s: " targetSolution sourceSolution
  log.Verbose @"Organization: %O" env.url
  env.logAuthentication log
  SolutionHelper.merge' env sourceSolution targetSolution log
  log.Info @"The solutions was merged successfully."
  
let pluginSteps (env: Environment) solution enable log = 
  let log = ConsoleLogger log
  logVersion log

  log.Info @"PluginSteps solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  env.logAuthentication log
  SolutionHelper.pluginSteps' env solution enable log
  let msg' = 
    enable |> function 
    | true -> "enabled"
    | false -> "disabled"
  log.Info @"The solution plugins was successfully %s" msg'

let enablePluginSteps (env: Environment) solutionName enable logLevel =
  let logLevel = logLevel ?| LogLevel.Verbose
  let enable = enable ?| true

  pluginSteps env solutionName enable logLevel

let workflow (env: Environment) solution enable log = 
  let log = ConsoleLogger log
  logVersion log
  log.Info @"WorkflowActivities solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  env.logAuthentication log
  SolutionHelper.workflow' env solution enable log
  let msg' = 
    enable |> function 
    | true -> "enabled"
    | false -> "disabled"
  log.WriteLine
    (LogLevel.Info, 
      @"The solution workflow activities was successfully " + msg')

let PublishAll (env: Environment) (timeOut: TimeSpan) =
  logVersion log
  log.Info @"Publish all customizations"
  log.Verbose @"Organization: %O" env.url
  let service = env.connect().GetService(timeOut)
  log.WriteLine(LogLevel.Verbose, @"Publishing customization")
  CrmDataHelper.publishAll service
  log.WriteLine(LogLevel.Verbose, @"All customizations was successfully published")
  
let export (env: Environment) solution location managed async (timeOut: TimeSpan) = 
  logVersion log
  log.Info @"Export solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  log.Verbose @"Managed solution: %O" managed
  log.Verbose @"Asynchronous export: %b" async
  env.logAuthentication log
  let service = env.connect().GetService(timeOut)
  match async with
  | false -> Export.exportSync service solution location managed
  | true -> Export.exportAsync service solution location managed
  
let import publishAfterImport (env: Environment) location (timeOut: TimeSpan) = 
  let solution, managed = CrmUtility.getSolutionInformationFromFile location
  logVersion log
  log.Info @"Import solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" location
  log.Verbose @"Managed solution: %O" managed
  log.Verbose @"Publish after Import: %O" publishAfterImport
  env.logAuthentication log
  let service = env.connect().GetService(timeOut)
  Import.execute service solution location managed |> ignore
  if publishAfterImport then
    Import.publish service managed

let extendSolution (env: Environment) solutionPath (timeOut: TimeSpan) = 
  let solution, _ = CrmUtility.getSolutionInformationFromFile solutionPath
  logVersion log
  log.Info @"Extend solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" solutionPath
  env.logAuthentication log
  let service = env.connect().GetService(timeOut)
  Extend.export service solution solutionPath

let preImportExtendedSolution (env: Environment) location (timeOut: TimeSpan) = 
  let solution, _ = CrmUtility.getSolutionInformationFromFile location
  logVersion log
  log.Info @"Pre-import extend solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" location
  env.logAuthentication log
  let service = env.connect().GetService(timeOut)
  Extend.preImport service solution location

let postImportExtendedSolution (env: Environment) location reassignWorkflows (timeOut: TimeSpan) = 
  let solution, _ = CrmUtility.getSolutionInformationFromFile location
  logVersion log
  log.Info @"Post-import extend solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" location
  env.logAuthentication log
  let service = env.connect().GetService(timeOut)
  Extend.postImport service solution location reassignWorkflows

let exportWithExtendedSolution (env: Environment) solution location managed async (timeOut: TimeSpan) = 
  logVersion log
  log.Info @"Export extended solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  log.Verbose @"Managed solution: %O" managed
  log.Verbose @"Asynchronous export: %b" async
  env.logAuthentication log
  SolutionHelper.exportWithExtendedSolution env solution location managed async timeOut

let importWithExtendedSolution reassignWorkflows (env: Environment) location (timeOut: TimeSpan) = 
  let solution, managed = CrmUtility.getSolutionInformationFromFile location
  logVersion log
  log.Info @"Import extended solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" location
  log.Verbose @"Managed solution: %O" managed
  env.logAuthentication log
  SolutionHelper.importWithExtendedSolution reassignWorkflows env solution location managed timeOut |> ignore

let importStandard (env: Environment) (activatePluginSteps: bool option) extended publishAfterImport reassignWorkflows pathToSolutionZip logLevel (timeOut: TimeSpan) =
  let logLevel = logLevel ?| LogLevel.Verbose
  let extended = extended ?| false

  match extended with
  | true  -> importWithExtendedSolution reassignWorkflows
  | false -> import publishAfterImport
  |> fun f -> f env pathToSolutionZip timeOut
      
  match activatePluginSteps with
  | Some true -> 
    let solutionName, _ = CrmUtility.getSolutionInformationFromFile pathToSolutionZip
    pluginSteps env solutionName true logLevel
  | _ -> ()

let exportStandard (env: Environment) solutionName outputDirectory managed extended async (timeOut: TimeSpan) =
  let extended = extended ?| false

  match extended with
  | true  -> exportWithExtendedSolution 
  | false -> export
  |> fun f -> f env solutionName outputDirectory managed async timeOut

let exportDiff fileLocation completeSolutionName temporarySolutionName async (dev:DG.Daxif.Environment) (prod:DG.Daxif.Environment) (timeOut: TimeSpan) = 
  log.Info "Starting diff export"
  Directory.CreateDirectory(fileLocation) |> ignore;
  // Export [complete solution] from DEV and PROD
  let ((devProxy, devSolution), (_, prodSolution)) = 
    [| dev; prod |]
    |> Array.Parallel.map (fun env -> 
      log.Verbose "Connecting to %s" env.name;
      let proxy = env.connect().GetService(timeOut)
      Directory.CreateDirectory(fileLocation + "/" + env.name) |> ignore;

      log.Verbose "Exporting solution '%s' from %s" completeSolutionName env.name;
      let sol = DiffFetcher.downloadSolution env (fileLocation + "/" + env.name + "/") completeSolutionName async timeOut
      DiffFetcher.unzip sol;
      (proxy, sol))
    |> function [| devSolution; prodSolution |] -> (devSolution, prodSolution) | _ -> failwith "Impossible"
  
  let publisherId = (DiffFetcher.fetchSolution devProxy completeSolutionName).Attributes.["publisherid"]
  let id = DiffAdder.createSolution devProxy temporarySolutionName publisherId
  
  try
    SolutionDiffHelper.diff devProxy temporarySolutionName devSolution prodSolution |> ignore
    // Export [partial solution] from DEV
    log.Verbose "Exporting solution '%s' from %s" temporarySolutionName dev.name;
    DiffFetcher.downloadSolution dev (fileLocation + "/") temporarySolutionName |> ignore
    // Delete [partial solution] on DEV
    log.Verbose "Deleting solution '%s'" temporarySolutionName;
    devProxy.Delete("solution", id);
    log.Info "Done exporting diff solution"
    temporarySolutionName
  with e -> 
    // Delete [partial solution] on DEV in case of error
    log.Verbose "Deleting solution '%s'" temporarySolutionName;
    devProxy.Delete("solution", id);
    failwith e.Message; 

let importDiff solutionZipPath completeSolutionName tempSolutionName (env:DG.Daxif.Environment) (timeOut: TimeSpan) = 
  log.Verbose "Connecting to environment %s" env.name;
  let proxy = env.connect().GetService(timeOut)
  let fileBytes = File.ReadAllBytes(solutionZipPath + "/" + tempSolutionName + ".zip")
  let stopWatch = System.Diagnostics.Stopwatch.StartNew()
  
  DiffAdder.executeImportRequestWithProgress proxy fileBytes

  let temp = solutionZipPath + "/" + tempSolutionName
  DiffFetcher.unzip temp;
  
  log.Verbose "Parsing TEMP customizations";
  DiffAdder.setWorkflowStates proxy temp
  
  let tempSolution = DiffFetcher.fetchSolution proxy tempSolutionName
  
  log.Info "Publishing changes"
  CrmDataHelper.publishAll proxy

  stopWatch.Stop()
  
  log.Info "Downtime: %.1f minutes" stopWatch.Elapsed.TotalMinutes;
  DiffAdder.transferSolutionComponents proxy tempSolution.Id completeSolutionName
  
  log.Verbose "Deleting solution '%s'" tempSolutionName
  proxy.Delete("solution", tempSolution.Id)

// TODO: 
let extract location customizations map project logLevel = 
  let log = ConsoleLogger logLevel
  logVersion log
  log.Verbose @"Path to file: %s" location
  SolutionHelper.extract' location customizations map project log logLevel
  log.Info @"The solution was extracted successfully"
  
// TODO: 
let pack location customizations map managed logLevel = 
  let log = ConsoleLogger logLevel
  logVersion log
  log.Verbose @"Path to file: %s" location
  SolutionHelper.pack' location customizations map managed log logLevel
  log.Info @"The solution was packed successfully"
  
// TODO: 
let updateServiceContext (env: Environment) location exe lcid log = 
  let log = ConsoleLogger log
  logVersion log
  log.Info @"Update service context:"
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Path to folder: %s" location
  env.logAuthentication log
  SolutionHelper.updateServiceContext' env location exe 
    lcid log
  log.Info @"The service context was updated successfully"
  
let updateCustomServiceContext (env: Environment) outputDirectory pathToExe log 
    solutions entities extraArgs = 
  let logger = ConsoleLogger log
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %O" env.url
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  env.logAuthentication logger

  logger.Info "Updating the C# context..."

  SolutionHelper.updateCustomServiceContext' env outputDirectory
    pathToExe logger solutions entities extraArgs
  logger.Info "The C# context was updated successfully"


let updateXrmMockupMetadata (env: Environment) outputDirectory pathToExe log 
    solutions entities extraArgs = 
  let logger = ConsoleLogger log
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %O" env.url
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  env.logAuthentication logger
  logger.Info "Updating XrmMockup Metadata..."

  SolutionHelper.updateXrmMockupMetadata' env outputDirectory
    pathToExe logger solutions entities extraArgs
  logger.Info "XrmMockup Metadata was updated successfully"
  

let updateTypeScriptContext (env: Environment) outputDirectory pathToExe log solutions entities extraArgs = 
  let logger = ConsoleLogger log
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %O" env.url
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  env.logAuthentication logger

  logger.Info "Updating the TypeScript context..."
  SolutionHelper.updateTypeScriptContext' env outputDirectory
    pathToExe logger solutions entities extraArgs

  logger.Info "The TypeScript context was updated successfully"

// Counts all the components in the solution.
let count (env: Environment) solution log =
  let log = ConsoleLogger log
  logVersion log
  log.Info @"Count components in: %s" solution
  log.Verbose @"Organization: %O" env.url
  env.logAuthentication log
  let _count = SolutionHelper.count' env solution
  log.Info @"The solution components were counted successfully"
  log.Info @"The solution contains %d components" _count