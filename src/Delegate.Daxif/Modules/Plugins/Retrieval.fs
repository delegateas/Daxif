module internal DG.Daxif.Modules.Plugin.Retrieval


open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages

open DG.Daxif.Common
open DG.Daxif.Common.Utility

open Domain
open CrmUtility


/// Retrieve registered plugins from CRM related to a certain assembly and solution
let retrieveRegistered proxy solutionId assemblyId =
  let typeMap =
    Query.pluginTypesByAssembly assemblyId 
    |> CrmDataHelper.retrieveAndMakeMap proxy getRecordName

  let validTypeGuids = typeMap |> Seq.map (fun kv -> kv.Value.Id) |> Set.ofSeq

  let steps =
    Query.pluginStepsBySolution solutionId 
    |> CrmDataHelper.retrieveMultiple proxy
    |> Seq.cache
    |> Seq.filter (fun e -> e.GetAttributeValue<EntityReference>("plugintypeid").Id |> validTypeGuids.Contains)

  let stepMap =
    steps |> makeMap getRecordName
    
  let stepGuidMap =
    steps |> makeMap (fun step -> step.Id)

  // Images do not have a unique name. It will be combined with the name of the parent step.
  let imageMap = 
    Query.pluginStepImagesBySolution solutionId 
    |> CrmDataHelper.retrieveMultiple proxy 
    |> Seq.choose (fun img -> 
      let stepId = img.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid").Id
      match stepGuidMap.TryFind stepId with
      | None      -> None
      | Some step ->
        let stepName =  getRecordName step
        let imageName = getRecordName img
        (sprintf "%s, %s" stepName imageName, img) |> Some
    )
    |> Map.ofSeq

  typeMap, stepMap, imageMap


/// Retrieve registered plugins from CRM under a given assembly
let retrieveRegisteredByAssembly proxy solutionId assemblyName =
  let targetAssembly = 
    Query.pluginAssembliesBySolution solutionId
    |> CrmDataHelper.retrieveMultiple proxy
    |> Seq.tryFind (fun a -> getRecordName a = assemblyName)
    

  match targetAssembly ?|> fun a -> a.Id with
  | None       -> Map.empty, Map.empty, Map.empty
  | Some asmId -> retrieveRegistered proxy solutionId asmId
  |> fun maps -> targetAssembly, maps


/// Retrieves the necessary SdkMessage and SdkMessageFilter guids 
/// for a collection of Steps
let getRelevantMessagesAndFilters proxy (steps: Step seq) =
  // Messages
  let messageRequests = 
    steps
    |> Seq.distinctBy (fun step -> step.eventOperation)
    |> Seq.map (fun step -> 
      step.eventOperation, 
      Query.sdkMessage step.eventOperation |> makeRetrieveMultiple
    ) 
    |> Array.ofSeq

  let messageMap = 
    messageRequests
    |> Array.map (snd >> toOrgReq)
    |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault
      (fun resp -> 
        let result = (resp.Response :?> RetrieveMultipleResponse)
        let message = result.EntityCollection.Entities.[0]
        let messageName = fst messageRequests.[resp.RequestIndex]
        messageName, message.Id
      )
    |> Map.ofArray

  // Filters
  let filterRequests = 
    steps
    |> Seq.distinctBy (fun step -> step.eventOperation, step.logicalName)
    |> Seq.map (fun step -> 
      let messageId = messageMap.[step.eventOperation]

      step.eventOperation, step.logicalName, messageId,
      Query.sdkMessageFilter step.logicalName messageId |> makeRetrieveMultiple
    )
    |> Array.ofSeq

  let finalMap =
    filterRequests
    |> Array.map (fun (_, _, _, q) -> q :> OrganizationRequest)
    |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault
      (fun resp -> 
        let result = (resp.Response :?> RetrieveMultipleResponse)
        let filter = result.EntityCollection.Entities.[0]
        let (op, logicalName, messageId, _) = filterRequests.[resp.RequestIndex]
        (op, logicalName), (messageId, filter.Id)
      )
    |> Map.ofArray
    
  finalMap
   

/// Retrieves the associated event operation for a given collection of step entities
/// and creates a map for the images to use
let getStepMap proxy (steps: Map<string, Entity>) =
  let messageGuids =
    steps
    |> Map.toSeq
    |> Seq.choose (fun (_, s) -> 
      s.GetAttributeValue<EntityReference>("sdkmessageid") 
      |> objToMaybe
      ?|> fun e -> e.Id)
    |> Set.ofSeq
    |> Array.ofSeq

  let eventOpMap =
    messageGuids
    |> Array.map (fun guid -> (makeRetrieve "sdkmessage" guid [| "name" |]) :> OrganizationRequest)
    |> CrmDataHelper.performAsBulkResultHandling proxy raiseExceptionIfFault
      (fun resp -> 
        let guid = messageGuids.[resp.RequestIndex]
        let entity = (resp.Response :?> RetrieveResponse).Entity
        let name = entity.GetAttributeValue<string>("name")
        guid, name
      )
    |> Map.ofArray

  steps
  |> Map.map (fun n e -> 
    e.Id, eventOpMap.[e.GetAttributeValue<EntityReference>("sdkmessageid").Id])