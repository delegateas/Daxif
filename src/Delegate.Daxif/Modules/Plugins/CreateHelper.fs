module internal DG.Daxif.Modules.Plugin.CreateHelper

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open System.Collections.Generic
open DG.Daxif.Common
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Common.Utility

open Domain
open CrmUtility
open CrmDataHelper
open Retrieval


/// Creates types and returns guid map for them.
let createTypes proxy solutionName typeDiff asmId targetTypes =
  let newTypes =
    typeDiff.adds 
    |> Map.toArray
    |> Array.map (fun (name, _) -> name, EntitySetup.createType asmId name)

  let orgTypeMap = targetTypes |> Map.map (fun _ (e: Entity) -> e.Id)
  
  // Create new types and add them to the map of already registered types
  newTypes 
  |>> Array.iter (fun (name, record) -> log.Info "Creating %s: %s" record.LogicalName name)
  |> Array.map (snd >> makeCreateReq >> attachToSolution solutionName >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault
    (fun e -> 
      let name = fst newTypes.[e.RequestIndex]
      let guid = (e.Response :?> CreateResponse).id
      name, guid)
  |> Array.fold (fun map (k, v) -> Map.add k v map) orgTypeMap


/// Creates steps and binds to matching plugin types. Returns a guid map for steps.
let createSteps proxy solutionName stepDiff orgSteps typeMap =
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

  let orgStepMap = getStepMap proxy orgSteps

  // Create new steps and add them to the map of already registered steps
  newSteps
  |>> Array.iter (fun (name, (record, _)) -> log.Info "Creating %s: %s" record.LogicalName name)
  |> Array.map (snd >> fst >> makeCreateReq >> attachToSolution solutionName >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault
    (fun e -> 
      let stepName, (_, eventOp) = newSteps.[e.RequestIndex]
      let stepId = (e.Response :?> CreateResponse).id
      stepName, (stepId, eventOp)
    )
  |> Array.fold (fun map (k, v) -> Map.add k v map) orgStepMap


/// Creates images and binds to matching steps
let createImages proxy solutionName imgDiff stepMap =
  imgDiff.adds 
  |> Map.toArray
  |> Array.map (fun (name, img) -> 
    let stepId, eventOp = Map.find img.stepName stepMap
    name, EntitySetup.createImage stepId eventOp img
  )
  |>> Array.iter (fun (name, record) -> log.Info "Creating %s: %s" record.LogicalName name)
  |> Array.map (snd >> makeCreateReq >> attachToSolution solutionName >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
  |> ignore

let createAPIReqs proxy solutionName prefix apiReqDiff targetReqAPIs apiMap =
  apiReqDiff.adds 
  |> Map.toArray
  |> Array.map (fun (name, req: RequestParameter) -> 
    let apiId = Map.find req.customApiName apiMap
    name, EntitySetup.createCustomAPIReq req (EntityReference("customapi", id = apiId)) prefix
  )
  |>> Array.iter (fun (name, record) -> log.Info "Creating %s: %s" record.LogicalName name)
  |> Array.map (snd >> makeCreateReq >> attachToSolution solutionName >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
  |> ignore

let createAPIResps proxy solutionName prefix apiRespDiff targetApiResps apiMap =
  apiRespDiff.adds
  |> Map.toArray
  |> Array.map (fun (name, resp: ResponseProperty) -> 
    let apiId = Map.find resp.customApiName apiMap
    name, EntitySetup.createCustomAPIResp resp (EntityReference("customapi", id = apiId)) prefix
  )
  |>> Array.iter (fun (name, record) -> log.Info "Creating %s: %s" record.LogicalName name)
  |> Array.map (snd >> makeCreateReq >> attachToSolution solutionName >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault ignore
  |> ignore

/// Creates custom apis and return guid map. 
let createAPIs proxy solutionName prefix apiDiff targetAPIs asmId =
  let apiArray = 
    apiDiff.adds 
    |> Map.toArray

  // Create plugintypes and store the ones created in dictionary
  // TODO This is not the place to do it. 
  // This should be done earlier so we catch it in the type diff.  
  let createdPluginTypes = new Dictionary<string, Guid>()
  apiArray
  |> Array.map (fun (_, api: Message) -> 
  api.pluginTypeName, EntitySetup.createType asmId api.pluginTypeName
  )
  |>> Array.iter (fun (pluginTypeName, record) -> log.Info "Creating %s: %s" record.LogicalName pluginTypeName)
  |> Array.map (fun (pluginTypeName, record) -> pluginTypeName,makeCreateReq record)
  |> Array.map (fun (pluginTypeName, req) -> pluginTypeName, attachToSolution solutionName req)
  |> Array.map (fun (pluginTypeName, req) -> pluginTypeName, toOrgReq req)
  |> Array.map (fun (pluginTypeName, req) -> (
    
    // Create plugin type if it does not exist
    match CrmDataInternal.Entities.pluginTypeExists proxy pluginTypeName with 
    | false -> 
      let typeResponse = CrmDataHelper.getResponse<CreateResponse> proxy req
      createdPluginTypes.Add(pluginTypeName, typeResponse.id)
    | true -> 
      let typeResponse = CrmDataInternal.Entities.retrievePluginType proxy pluginTypeName
      createdPluginTypes.Add(pluginTypeName, typeResponse.Id)
    ))
  |> ignore

  let orgApisMap = targetAPIs |> Map.map (fun _ (e: Entity) -> e.Id)

  // Create custom api's and store guid maps

  let newApis = 
    apiArray
    |> Array.map (fun (_, api: Message) -> 
       match createdPluginTypes.TryGetValue api.pluginTypeName with
       | true, value -> api.name, EntitySetup.createCustomAPI (api) (EntityReference("plugintype", id = value)) (prefix)
       | _           -> null, null
       )

  newApis
  |> Array.filter( fun (name, record) -> name <> null)
  |>> Array.iter (fun (name, record) -> log.Info "Creating %s: %s" record.LogicalName name)
  |> Array.map (snd >> makeCreateReq >> attachToSolution solutionName >> toOrgReq)
  |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault 
    (fun e -> 
      let name = fst newApis.[e.RequestIndex]
      let guid = (e.Response :?> CreateResponse).id
      name, guid)
  |> Array.fold (fun map (k, v) -> Map.add k v map) orgApisMap



