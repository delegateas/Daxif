module internal DG.Daxif.Modules.Plugin.MainHelper

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility

open Domain
open CrmUtility
open InternalUtility
open CrmDataHelper
open Retrieval

/// Transforms plugins from source to maps with names as keys
let localToMaps (plugins: Plugin seq) (customAPIs: CustomAPI seq) =
  let pluginTypeMap, stepMap, imageMap = 
    plugins
    |> Seq.fold (fun (typeMap, stepMap, imageMap) p ->
      let newTypeMap = if Map.containsKey p.TypeKey typeMap then typeMap else Map.add p.TypeKey p typeMap
      let newStepMap = Map.add p.StepKey p.step stepMap
      let newImageMap = p.ImagesWithKeys |> Seq.fold (fun acc (k,v) -> Map.add k v acc) imageMap

      newTypeMap, newStepMap, newImageMap
    ) (Map.empty, Map.empty, Map.empty)

  let customApiTypeMap, customApiMap, reqParamMap, respPropMap = 
    customAPIs
    |> Seq.fold (fun (typeMap, customApiMap, reqParamMap, respPropMap) c ->
      let newTypeMap = if Map.containsKey c.TypeKey typeMap then typeMap else Map.add c.TypeKey c typeMap
      let newcustomApiMap = Map.add c.Key c.message customApiMap
      let newReqParamMap = c.RequestParametersWithKeys |> Seq.fold (fun acc x -> Map.add x.name x acc) reqParamMap
      let newRespPropMap = c.ResponsePropertiesWithKeys |> Seq.fold (fun acc x -> Map.add x.name x acc) respPropMap

      newTypeMap, newcustomApiMap, newReqParamMap, newRespPropMap
    ) (Map.empty, Map.empty, Map.empty, Map.empty)
  
  // Convert CustomApi to Plugin to merge maps
  let apiTypeMapPlugins = 
    customApiTypeMap
    |> Map.map (fun (name) (api) -> 
    {
    step = {
        pluginTypeName = name
        executionStage = 1
        eventOperation = ""
        logicalName = ""
        deployment = 1
        executionMode = 1
        name = name
        executionOrder = 1
        filteredAttributes = ""
        userContext = Guid.Empty
        }
    images = Seq.empty 
    })
  
  // Merge pluginTypeMap and customApiTypeMap 
  let mergedTypeMap = Map.fold (fun acc key value -> Map.add key value acc) pluginTypeMap apiTypeMapPlugins

  mergedTypeMap, stepMap, imageMap, customApiTypeMap, customApiMap, reqParamMap, respPropMap

/// Determine which operation we want to perform on the assembly
let determineOperation (asmReg: AssemblyRegistration option) (asmLocal) : AssemblyOperation * Guid =
  match asmReg with
  | Some asm when Compare.registeredIsSameAsLocal asmLocal (Some asm) -> Unchanged, asm.id
  | Some asm -> Update, asm.id
  | None     -> Create, Guid.Empty

/// Update or create assembly
let ensureAssembly proxy solutionName asmLocal maybeAsm =
  match determineOperation maybeAsm asmLocal with
  | Unchanged, id ->
      log.Info "No changes to assembly %s detected" asmLocal.dllName
      id
  | Update, id ->
      let asmEntity = EntitySetup.createAssembly asmLocal.dllName asmLocal.dllPath asmLocal.assembly asmLocal.hash asmLocal.isolationMode
      asmEntity.Id <- id
      CrmDataHelper.getResponse<UpdateResponse> proxy (makeUpdateReq asmEntity) |> ignore
      log.Info "Updating %s: %s" asmEntity.LogicalName asmLocal.dllName
      id
  | Create, _ ->
      let asmEntity = EntitySetup.createAssembly asmLocal.dllName asmLocal.dllPath asmLocal.assembly asmLocal.hash asmLocal.isolationMode
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
let performDelete proxy imgDiff stepDiff typeDiff apiDiff apiReqDiff apiRespDiff (sourceAPITypeMaps:Map<string,CustomAPI>) =
  // TODO Do this different correlates with note in CreateHelper.fs
  // Remove typeDiff.deletes which are to be used by Custom API
  let newTypeDeletes = 
    typeDiff.deletes
    |> Map.toArray
    |> Array.filter (fun (name, entity) -> not (sourceAPITypeMaps.ContainsKey name))
    |> Map
  
  performMapDelete proxy apiRespDiff.deletes
  performMapDelete proxy apiReqDiff.deletes
  performMapDelete proxy apiDiff.deletes
  performMapDelete proxy imgDiff.deletes 
  performMapDelete proxy stepDiff.deletes
  performMapDelete proxy newTypeDeletes


/// Updates with changes to the plugin configuration
let update proxy imgDiff stepDiff =
  let imgUpdates = 
    imgDiff.differences 
    |> Map.toArray
    |> Array.map (fun (name, (img, e: Entity)) -> name, EntitySetup.updateImage img e)

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
let create proxy solutionName prefix imgDiff stepDiff apiDiff apiReqDiff apiRespDiff typeDiff asmId targetTypes targetSteps targetAPIs targetApiReqs targetApiResps =
  let types = CreateHelper.createTypes proxy solutionName typeDiff asmId targetTypes
  
  types
  |> CreateHelper.createSteps proxy solutionName stepDiff targetSteps
  |> CreateHelper.createImages proxy solutionName imgDiff

  let apis = CreateHelper.createAPIs proxy solutionName prefix apiDiff targetAPIs asmId types
  
  apis
  |> CreateHelper.createAPIReqs proxy solutionName prefix apiReqDiff targetApiReqs

  apis 
  |> CreateHelper.createAPIResps proxy solutionName prefix apiRespDiff targetApiResps
  

/// Load a local assembly and validate its plugins
let loadAndValidateAssembly proxy dllPath isolationMode =
  log.Verbose "Loading local assembly and its plugins"
  let asmLocal = PluginDetection.getAssemblyContextFromDll dllPath isolationMode
  log.Verbose "Local assembly version %s loaded" (asmLocal.version |> versionToString)

  log.Verbose "Validating plugins to be registered"
  match Validation.validatePlugins proxy asmLocal.plugins with
  | Validation.Invalid err  -> failwith err
  | Validation.Valid _      -> ()
  log.Verbose "Validation completed"

  asmLocal


/// Analyzes local and remote registrations and returns the information about each of them
let analyze proxyGen dllPath solutionName isolationMode =
  let proxy = proxyGen()

  let asmLocal = loadAndValidateAssembly proxy dllPath isolationMode
  let solutionId = CrmDataInternal.Entities.retrieveSolutionId proxy solutionName
  let _id, prefix = CrmDataInternal.Entities.retrieveSolutionIdAndPrefix proxy solutionName
  let asmReg, pluginsReg = Retrieval.retrieveRegisteredByAssembly proxy solutionId asmLocal.dllName
  let pluginsLocal = localToMaps asmLocal.plugins asmLocal.customAPIs
    
  asmLocal, asmReg, pluginsLocal, pluginsReg, prefix


/// Performs a full synchronization of plugins
let performSync proxy solutionName prefix asmCtx asmReg (sourceTypes, sourceSteps, sourceImgs, sourceAPITypeMaps, sourceApis, sourceReqParams, sourceRespProps) (targetTypes, targetSteps, targetImgs, targetApis, targetReqParams, targetRespProps) =
  log.Info "Starting plugin synchronization"
 
  // Find differences
  let typeDiff = mapDiff sourceTypes targetTypes Compare.pluginType
  let stepDiff = mapDiff sourceSteps targetSteps Compare.step
  let imgDiff = mapDiff sourceImgs targetImgs Compare.image
  let apiDiff = mapDiff sourceApis targetApis Compare.api
  let apiReqDiff = mapDiff sourceReqParams targetReqParams Compare.apiReq
  let apiRespDiff = mapDiff sourceRespProps targetRespProps Compare.apiResp

  // Perform sync
  log.Info "Deleting removed registrations"
  performDelete proxy imgDiff stepDiff typeDiff apiDiff apiReqDiff apiRespDiff sourceAPITypeMaps

  log.Info "Creating/updating assembly"
  let asmId = ensureAssembly proxy solutionName asmCtx asmReg
  
  log.Info "Updating existing registrations"
  update proxy imgDiff stepDiff

  log.Info "Creating new registrations"
  create proxy solutionName prefix imgDiff stepDiff apiDiff apiReqDiff apiRespDiff typeDiff asmId targetTypes targetSteps targetApis targetReqParams targetRespProps

  log.Info "Plugin synchronization was completed successfully"