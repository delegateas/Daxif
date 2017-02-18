namespace DG.Daxif.Common

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

type CrmDataHelper = 

  /// Execute a request and expect a response of a certain type
  static member getResponse<'T when 'T :> OrganizationResponse> (proxy: OrganizationServiceProxy) request =
    proxy.Timeout <- TimeSpan(1,0,0)
    (proxy.Execute(request)) :?> 'T

  static member whoAmI proxy =
    let req = WhoAmIRequest()
    let resp = CrmDataHelper.getResponse<WhoAmIResponse> proxy req
    resp.UserId

  /// Perform requests as bulk
  static member performAsBulk proxy (reqs: OrganizationRequest[]) = 
    reqs
    |> FSharpCoreExt.Array.chunk 200
    |> Array.map (fun splitReqs ->
      let req = ExecuteMultipleRequest()
      req.Requests <- OrganizationRequestCollection()
      req.Requests.AddRange(splitReqs)
      req.Settings <- ExecuteMultipleSettings()
      req.Settings.ContinueOnError <- true
      req.Settings.ReturnResponses <- true
      (CrmDataHelper.getResponse<ExecuteMultipleResponse> proxy req).Responses
    ) |> Seq.concat
    |> Array.ofSeq


  /// Perform requests as bulk. Any faults will cause an exception to be thrown.
  /// Returns the value returned by the transform of the response
  static member performAsBulkResultHandling proxy faultHandler resultTransform  =
    CrmDataHelper.performAsBulk proxy
    >> Array.map (fun resp -> 
      faultHandler resp.Fault
      resultTransform resp)

  /// Publish all
  static member publishAll proxy = 
    let (proxy : OrganizationServiceProxy) = proxy
    let req = Messages.PublishAllXmlRequest()
    proxy.Timeout <- new TimeSpan(1, 0, 0) // 1 hour timeout for PublishAll
    CrmDataHelper.getResponse<Messages.PublishAllXmlResponse> proxy req |> ignore


  /// Retrieve multiple with automatic pagination
  static member retrieveMultiple proxy (query:QueryExpression) = 
    query.PageInfo <- PagingInfo()

    let rec retrieveMultiple' 
      (proxy:OrganizationServiceProxy) (query:QueryExpression) page cookie =
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

  static member retrieveAndMakeMap proxy keyFunc =
    CrmDataHelper.retrieveMultiple proxy >> makeMap keyFunc

  /// Execute a certain request
  static member execute (proxy: OrganizationServiceProxy, request: CreateRequest, ?parameters) =
    match parameters with
    | Some ps -> ps |> Seq.iter (fun (k,v) -> request.Parameters.Add(k, v))
    | None -> ()
    CrmDataHelper.getResponse<CreateResponse> proxy request

  /// Execute a certain request
  static member execute (proxy: OrganizationServiceProxy, request: UpdateRequest) =
    CrmDataHelper.getResponse<UpdateResponse> proxy request

  /// Execute a certain request
  static member execute (proxy: OrganizationServiceProxy, request: DeleteRequest) =
    CrmDataHelper.getResponse<DeleteResponse> proxy request

  /// Execute a certain request
  static member execute (proxy: OrganizationServiceProxy, request: RetrieveMultipleRequest) =
    CrmDataHelper.getResponse<RetrieveMultipleResponse> proxy request