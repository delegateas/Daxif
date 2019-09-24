module DG.Daxif.Common.CrmDataHelper

open System
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Query
open DG.Daxif
open DG.Daxif.Common.Utility

open CrmUtility
open Microsoft.Crm.Sdk.Messages

type RetrieveSelect = 
  | All
  | OnlyId
  | Fields of seq<string>
  with 
    member x.columnSet =
      match x with
      | All      -> ColumnSet(true)
      | OnlyId   -> ColumnSet(null)
      | Fields x -> ColumnSet(Array.ofSeq x)


/// Makes an update request from an entity object
let makeUpdateReq e = 
  let req = UpdateRequest()
  req.Target <- e
  req

/// Makes an create request from an entity object
let makeCreateReq e = 
  let req = CreateRequest()
  req.Target <- e
  req

/// Makes an delete request from an entity reference
let makeDeleteReq (e: Entity) = 
  let req = DeleteRequest()
  req.Target <- e.ToEntityReference()
  req


/// Makes an delete request from an entity reference
let makeRetrieve (logicalName: string) (guid: Guid) (select: RetrieveSelect) = 
  let req = RetrieveRequest()
  req.Target <- EntityReference(logicalName, guid)
  req.ColumnSet <- select.columnSet
  req

/// Makes an delete request from an entity reference
let makeRetrieveMultiple (q: QueryExpression) = 
  let req = RetrieveMultipleRequest()
  req.Query <- q
  req


/// Execute a request and expect a response of a certain type
let getResponse<'T when 'T :> OrganizationResponse> (proxy: IOrganizationService) request =
  (proxy.Execute(request)) :?> 'T


/// Execute a request with given parameters
let getResponseWithParams<'T when 'T :> OrganizationResponse> proxy (request: OrganizationRequest) parameters : 'T =
  parameters |> Seq.iter (fun (k,v) -> request.Parameters.Add(k, v))
  getResponse<'T> proxy request

/// Execute a request and ignore the response
let executeRequest proxy request =
  getResponse proxy request |> ignore


/// Perform requests as bulk
let performAsBulk proxy (reqs: OrganizationRequest[]) = 
  reqs
  |> FSharpCoreExt.Array.chunk 200
  |> Array.map (fun splitReqs ->
    let req = ExecuteMultipleRequest()
    req.Requests <- OrganizationRequestCollection()
    req.Requests.AddRange(splitReqs)
    req.Settings <- ExecuteMultipleSettings()
    req.Settings.ContinueOnError <- true
    req.Settings.ReturnResponses <- true
    (getResponse<ExecuteMultipleResponse> proxy req).Responses
  ) |> Seq.concat
  |> Array.ofSeq


/// Perform requests as bulk. Any faults will cause an exception to be thrown.
/// Returns the value returned by the transform of the response
let performAsBulkResultHandling proxy faultHandler resultTransform  =
  performAsBulk proxy
  >> Array.map (fun resp -> 
    faultHandler resp.Fault
    resultTransform resp)


let bulkRetrieveMultiple proxy =
  Seq.map (makeRetrieveMultiple >> toOrgReq)
  >> Array.ofSeq
  >> performAsBulk proxy
  >> Array.map (fun resp -> (resp.Response :?> RetrieveMultipleResponse).EntityCollection.Entities)
  >> Seq.concat
  >> Array.ofSeq


/// Retrieve
let retrieve (proxy: IOrganizationService) logicalName guid (select: RetrieveSelect) =
  proxy.Retrieve(logicalName, guid, select.columnSet)

/// Exists
let exists (proxy: IOrganizationService) logicalName guid =
  try 
    let e = retrieve proxy logicalName guid RetrieveSelect.OnlyId
    e.Id <> Guid.Empty
  with _ -> false


/// Retrieve multiple with automatic pagination
let retrieveMultiple proxy (query:QueryExpression) = 
  query.PageInfo <- PagingInfo()

  let rec retrieveMultiple' 
    (proxy:IOrganizationService) (query: QueryExpression) page cookie =
    seq {
        query.PageInfo.PageNumber <- page
        query.PageInfo.PagingCookie <- cookie
        let resp = proxy.RetrieveMultiple(query)
        yield! resp.Entities

        match resp.MoreRecords with
        | true -> yield! retrieveMultiple' proxy query (page + 1) resp.PagingCookie
        | false -> ()
    }
  retrieveMultiple' proxy query 1 null
  
/// Retrieve multiple which returns the first match, or raises an exception if no matches were found
let retrieveFirstMatch (proxy: IOrganizationService) (query: QueryExpression) = 
  query.TopCount <- Nullable(1)
  proxy.RetrieveMultiple(query).Entities
  |> Seq.tryHead
  |> function
  | Some r -> r
  | None   -> 
    failwithf "No entities of type '%s' found with the given query.\n%s" 
      query.EntityName 
      (queryExpressionToString query)

/// Retrieve multiple and turns the entities into a map based on the given key function
let retrieveAndMakeMap proxy keyFunc =
  retrieveMultiple proxy >> makeMap keyFunc


/// Publish all
let publishAll (proxy: IOrganizationService) = 
  let req = Messages.PublishAllXmlRequest()
  getResponse<Messages.PublishAllXmlResponse> proxy req |> ignore


/// Retrieve current user id
let whoAmI proxy =
  let req = WhoAmIRequest()
  let resp = getResponse<WhoAmIResponse> proxy req
  resp.UserId


/// Retrieves the solution with the given name
let retrieveSolution proxy (solutionName: string) (retrieveSelect: RetrieveSelect) =
  let f = FilterExpression()
  f.AddCondition(ConditionExpression(@"uniquename", ConditionOperator.Equal, solutionName))
  let q = QueryExpression("solution")
  q.ColumnSet <- retrieveSelect.columnSet
  q.Criteria <- f
  retrieveFirstMatch proxy q