module internal DG.Daxif.Modules.Plugin.Sync

open System.IO
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages

open DG.Daxif.Common
open DG.Daxif.Common.Utility

open Domain
open CrmUtility
open Retrieval

/// Transforms plugins from source to maps with names as keys
let localToMaps (plugins: Plugin seq) =
  plugins
  |> Seq.fold (fun (typeMap, stepMap, imageMap) p ->
    let typeMap' = if Map.containsKey p.TypeKey typeMap then typeMap else Map.add p.TypeKey p typeMap
    let stepMap' = Map.add p.StepKey p.step stepMap
    let imageMap' = p.ImagesWithKeys |> Seq.fold (fun acc (k,v) -> Map.add k v acc) imageMap

    typeMap', stepMap', imageMap'
  ) (Map.empty, Map.empty, Map.empty)


/// Update or create assembly
let ensureAssembly proxy solutionName asmCtx maybeAsm =
  match Compare.assembly asmCtx maybeAsm with
  | true  -> maybeAsm.Value.Id
  | false ->
    let asmEntity = EntitySetup.createAssembly asmCtx.dllName asmCtx.dllPath asmCtx.assembly asmCtx.hash asmCtx.isolationMode
    
    match maybeAsm with
    | Some asm ->
      asmEntity.Id <- asm.Id
      CrmDataHelper.execute(proxy, makeUpdateReq asmEntity) |> ignore
      asm.Id

    | None     -> 
      CrmDataHelper.execute(proxy, makeCreateReq asmEntity, [ "SolutionUniqueName", solutionName ]).id


/// Deletes obsolete entities in plugin configuration
let delete proxy imgDiff stepDiff typeDiff =
  let performDelete = 
    Map.toArray
    >> Array.map (snd >> makeDeleteReq >> toOrgReq)
    >> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
    >> ignore

  performDelete imgDiff.deletes 
  performDelete stepDiff.deletes
  performDelete typeDiff.deletes


/// Updates with changes to the plugin configuration
let update proxy imgDiff stepDiff =
  let imgUpdates = 
    imgDiff.differences 
    |> Map.toArray
    |> Array.map (fun (_, (img, e: Entity)) -> EntitySetup.updateImage e.Id img)

  let stepUpdates =
    stepDiff.differences
    |> Map.toArray
    |> Array.map (fun (_, (step, e: Entity)) -> EntitySetup.updateStep e.Id step)

  Array.concat [| imgUpdates; stepUpdates |]
  |> Array.map (makeUpdateReq >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
  |> ignore


/// Creates types and returns guid map for them.
let createTypes proxy typeDiff asmId targetTypes =
  let newTypes =
    typeDiff.adds 
    |> Map.toArray
    |> Array.map (fun (name, _) -> name, EntitySetup.createType asmId name)

  let orgTypeMap = targetTypes |> Map.map (fun _ (e: Entity) -> e.Id)
  let typeMap =
    newTypes 
    |> Array.map (snd >> makeCreateReq >> toOrgReq)
    |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault
      (fun e -> 
        let name = fst newTypes.[e.RequestIndex]
        let guid = (e.Response :?> CreateResponse).id
        name, guid)
    |> Array.fold (fun map (k,v) -> Map.add k v map) orgTypeMap

  typeMap


/// Creates steps and binds to matching plugin types. Returns a guid map for steps.
let createSteps proxy stepDiff targetSteps typeMap =
  let stepsArray =
    stepDiff.adds
    |> Map.toArray

  // Get the necessary SdkMessage and SdkMessageFilter guids for the new steps
  let messageFilterMap = 
    stepsArray
    |> Array.map snd
    |> getRelevantMessagesAndFilters proxy 

  let newSteps =
    stepsArray
    |> Array.map (fun (name, step) -> 
      let typeId = Map.find step.pluginTypeName typeMap
      let messageId, filterId = messageFilterMap.[step.eventOperation, step.logicalName]
      let messageRecord = EntitySetup.createStep typeId messageId filterId name step

      name, (messageRecord, step.eventOperation)
    )

  // Perform creates and add to guid map
  let orgStepMap = getStepMap proxy targetSteps

  let stepMap =
    newSteps 
    |> Array.map (snd >> fst >> makeCreateReq >> toOrgReq)
    |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault
      (fun e -> 
        let stepName, (_, eventOp) = newSteps.[e.RequestIndex]
        let stepId = (e.Response :?> CreateResponse).id
        stepName, (stepId, eventOp)
      )
    |> Array.fold (fun map (k,v) -> Map.add k v map) orgStepMap

  stepMap


/// Creates images and binds to matching steps
let createImages proxy imgDiff stepMap =
  imgDiff.adds 
  |> Map.toArray
  |> Array.map (fun (_, img) -> 
    let stepId, eventOp = Map.find img.stepName stepMap
    EntitySetup.createImage stepId eventOp img
  )
  |> Array.map (makeCreateReq >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
  |> ignore


/// Creates additions to the plugin configuration in the correct order and
/// passes guid maps to next step in process
let create proxy imgDiff stepDiff typeDiff asmId targetTypes targetSteps =
  createTypes proxy typeDiff asmId targetTypes
  |> createSteps proxy stepDiff targetSteps
  |> createImages proxy imgDiff
