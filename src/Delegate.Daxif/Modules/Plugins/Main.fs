module DG.Daxif.Modules.Plugin.Main

open DG.Daxif
open DG.Daxif.Modules.Plugin
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

open Domain

/// Load a local assembly and validate its plugins
let loadAndValidateAssembly proxyGen projectPath dllPath isolationMode =
  log.Verbose "Loading local assembly and it's plugins"
  let asm = PluginDetection.getAssemblyContextFromDll projectPath dllPath isolationMode
  log.Verbose "Local assembly loaded"

  log.Verbose "Validating plugins to be registered"
  match Validation.validatePlugins proxyGen asm.plugins with
  | Validation.Invalid err -> failwith err
  | Validation.Valid _ -> ()
  log.Verbose "Validation completed"

  asm


/// Analyzes local and remote registrations and returns the information about each of them
let analyze proxyGen projectPath dllPath solutionName isolationMode =
  use proxy = proxyGen()

  let asmCtx = loadAndValidateAssembly proxyGen dllPath projectPath isolationMode
  let solution = CrmDataInternal.Entities.retrieveSolution proxy solutionName

  let asm, registered = Retrieval.retrieveRegisteredByAssembly proxy solution.Id asmCtx.dllName
  let local = Sync.localToMaps asmCtx.plugins
    
  asmCtx, asm, local, registered


/// Performs a full synchronization of plugins
let performSync proxy solutionName asmCtx asm (sourceTypes, sourceSteps, sourceImgs) (targetTypes, targetSteps, targetImgs) =
  log.Info "Starting plugin synchronization"
  
  // Find differences
  let typeDiff = mapDiff sourceTypes targetTypes Compare.pluginType
  let stepDiff = mapDiff sourceSteps targetSteps Compare.step
  let imgDiff = mapDiff sourceImgs targetImgs Compare.image

  // Perform sync
  log.Info "Deleting old registrations"
  Sync.delete proxy imgDiff stepDiff typeDiff

  log.Info "Creating/updating assembly"
  let asmId = Sync.ensureAssembly proxy solutionName asmCtx asm
  
  log.Info "Updating existing registrations"
  Sync.update proxy imgDiff stepDiff

  log.Info "Creating new registrations"
  Sync.create proxy imgDiff stepDiff typeDiff asmId targetTypes targetSteps

  log.Info "Plugin synchronization was successful"


/// Main function
let syncSolution proxyGen projectPath dllPath solutionName isolationMode dryRun =
  log.Info "Action: Plugin synchronization"

  log.Info "Comparing plugins registered in CRM versus those found in your local code"
  let asmCtx, asm, local, registered = analyze proxyGen projectPath dllPath solutionName isolationMode

  match dryRun with
  | false -> 
    performSync (proxyGen()) solutionName asmCtx asm local registered
    
  | true  ->
    log.Info "***** Dry run *****"
    let regTypes, regSteps, regImages = registered
    let localTypes, localSteps, localImages = local
    printMergePartition "Types" localTypes regTypes Compare.pluginType log 
    printMergePartition "Steps" localSteps regSteps Compare.step log
    printMergePartition "Images" localImages regImages Compare.image log

  local, registered
    