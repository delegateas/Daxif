module internal DG.Daxif.Modules.Plugin.MainHelper

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages

open DG.Daxif.Common
open DG.Daxif.Common.Utility

open Domain
open CrmUtility
open InternalUtility
open CrmDataHelper
open Retrieval

/// Transforms plugins from source to maps with names as keys
let localToMaps (plugins: Plugin seq) =
  plugins
  |> Seq.fold (fun (typeMap, stepMap, imageMap) p ->
    let newTypeMap = if Map.containsKey p.TypeKey typeMap then typeMap else Map.add p.TypeKey p typeMap
    let newStepMap = Map.add p.StepKey p.step stepMap
    let newImageMap = p.ImagesWithKeys |> Seq.fold (fun acc (k,v) -> Map.add k v acc) imageMap

    newTypeMap, newStepMap, newImageMap
  ) (Map.empty, Map.empty, Map.empty)


/// Update or create assembly
let ensureAssembly proxy solutionName asmLocal maybeAsm =
  match Compare.assembly asmLocal maybeAsm with
  | true  -> maybeAsm.Value.id
  | false ->
    let asmEntity = EntitySetup.createAssembly asmLocal.dllName asmLocal.dllPath asmLocal.assembly asmLocal.hash asmLocal.isolationMode
    
    match maybeAsm with
    | Some asmReg ->
      asmEntity.Id <- asmReg.id
      CrmDataHelper.getResponse<UpdateResponse> proxy (makeUpdateReq asmEntity) |> ignore
      log.Info "Updating %s: %s" asmEntity.LogicalName asmLocal.dllName
      asmReg.id

    | None        -> 
      log.Info "Creating %s: %s" asmEntity.LogicalName asmLocal.dllName
      CrmDataHelper.getResponseWithParams<CreateResponse> proxy (makeCreateReq asmEntity) [ "SolutionUniqueName", solutionName ]
      |> fun r -> r.id

// Deletes records in given map
let performMapDelete proxy map =
  map 
  |>> Map.iter (fun k (v: Entity) -> log.Info "Deleting %s: %s" v.LogicalName k)
  |> Map.toArray
  |> Array.map (snd >> makeDeleteReq >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
  |> ignore

/// Deletes obsolete records in current plugin configuration
let performDelete proxy imgDiff stepDiff typeDiff =
  // Delete sequentially because of dependencies to parent entity
  performMapDelete proxy imgDiff.deletes 
  performMapDelete proxy stepDiff.deletes
  performMapDelete proxy typeDiff.deletes


/// Updates with changes to the plugin configuration
let update proxy imgDiff stepDiff =
  let imgUpdates = 
    imgDiff.differences 
    |> Map.toArray
    |> Array.map (fun (name, (img, e: Entity)) -> name, EntitySetup.updateImage e.Id (e.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid")) img)

  let stepUpdates =
    stepDiff.differences
    |> Map.toArray
    |> Array.map (fun (name, (step, e: Entity)) -> name, EntitySetup.updateStep e.Id step)

  let updates = Array.concat [| imgUpdates; stepUpdates |]

  updates 
  |>> Array.iter (fun (name, record) -> log.Info "Updating %s: %s" record.LogicalName name)
  |> Array.map (snd >> makeUpdateReq >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
  |> ignore
  

/// Creates additions to the plugin configuration in the correct order and
/// passes guid maps to next step in process
let create proxy solutionName imgDiff stepDiff typeDiff asmId targetTypes targetSteps =
  CreateHelper.createTypes proxy solutionName typeDiff asmId targetTypes
  |> CreateHelper.createSteps proxy solutionName stepDiff targetSteps
  |> CreateHelper.createImages proxy solutionName imgDiff


/// Load a local assembly and validate its plugins
let loadAndValidateAssembly proxyGen projectPath dllPath isolationMode ignoreOutdatedAssembly =
  log.Verbose "Loading local assembly and it's plugins"
  let asmLocal = PluginDetection.getAssemblyContextFromDll projectPath dllPath isolationMode ignoreOutdatedAssembly
  log.Verbose "Local assembly loaded"

  log.Verbose "Validating plugins to be registered"
  match Validation.validatePlugins proxyGen asmLocal.plugins with
  | Validation.Invalid err  -> failwith err
  | Validation.Valid _      -> ()
  log.Verbose "Validation completed"

  asmLocal


/// Analyzes local and remote registrations and returns the information about each of them
let analyze proxyGen projectPath dllPath solutionName isolationMode ignoreOutdatedAssembly =
  use proxy = proxyGen()

  let asmLocal = loadAndValidateAssembly proxyGen projectPath dllPath isolationMode ignoreOutdatedAssembly
  let solution = CrmDataInternal.Entities.retrieveSolutionId proxy solutionName

  let asmReg, pluginsReg = Retrieval.retrieveRegisteredByAssembly proxy solution.Id asmLocal.dllName
  let pluginsLocal = localToMaps asmLocal.plugins
    
  asmLocal, asmReg, pluginsLocal, pluginsReg


/// Performs a full synchronization of plugins
let performSync proxy solutionName asmCtx asmReg (sourceTypes, sourceSteps, sourceImgs) (targetTypes, targetSteps, targetImgs) =
  log.Info "Starting plugin synchronization"
  
  // Find differences
  let typeDiff = mapDiff sourceTypes targetTypes Compare.pluginType
  let stepDiff = mapDiff sourceSteps targetSteps Compare.step
  let imgDiff = mapDiff sourceImgs targetImgs Compare.image

  // Perform sync
  log.Info "Deleting removed registrations"
  performDelete proxy imgDiff stepDiff typeDiff

  log.Info "Creating/updating assembly"
  let asmId = ensureAssembly proxy solutionName asmCtx asmReg
  
  log.Info "Updating existing registrations"
  update proxy imgDiff stepDiff

  log.Info "Creating new registrations"
  create proxy solutionName imgDiff stepDiff typeDiff asmId targetTypes targetSteps

  log.Info "Plugin synchronization was completed successfully"