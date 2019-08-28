module internal DG.Daxif.Modules.Solution.Import

open System
open System.IO
open System.Text
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.CrmDataInternal
open DG.Daxif.Common.Utility
open InternalUtility
open Domain
open CrmUtility
open CrmDataHelper
open Utility
open Microsoft.Xrm.Sdk.Client
open Microsoft.Crm.Sdk
open DG.Daxif.Modules.Serialization


let startImportJob p req =
  match CrmDataInternal.Info.version p with
  | (_, CrmReleases.CRM2011) -> 
    p.Execute(req) :?> Messages.ImportSolutionResponse |> ignore
    log.WriteLine(LogLevel.Verbose,@"Import job Started")
    None
  | (_, _) -> 
    log.WriteLine(LogLevel.Verbose,@"Asynchronous import job started")
    req |> makeAsyncRequest
    |> p.Execute :?> Messages.ExecuteAsyncResponse 
    |> fun r -> Some(r.AsyncJobId)

let importJobEntityHasFailure (importJobEntity: Entity) =
  let data = importJobEntity.GetAttributeValue<string>("data")
  data.Contains("<result result=\"failure\"")

let getAsyncImportJobStatus p asyncJobId =
  try
    match Info.retrieveAsyncJobState p asyncJobId with
    | AsyncJobState.Succeeded ->
      Domain.ImportState.Succeeded
    | AsyncJobState.Failed 
    | AsyncJobState.Canceled ->
      Domain.ImportState.Failed
    | AsyncJobState.Canceling
    | AsyncJobState.Pausing
    | AsyncJobState.InProgress -> 
      Domain.ImportState.InProgress
    | AsyncJobState.WaitingForResources
    | AsyncJobState.Waiting -> 
      Domain.ImportState.NotStarted
    | _ -> Domain.ImportState.InProgress
  with _ -> Domain.ImportState.InProgress

let getSyncImportJobStatus (importEntity: Entity) =
  match importEntity.Attributes.Contains("completedon") with
  | false -> Domain.ImportState.InProgress
  | true -> 
    match importJobEntityHasFailure importEntity with
    | false -> Domain.ImportState.Succeeded
    | true -> Domain.ImportState.Failed

let getImportJobStatus' p importJob asyncJobId = 
  let importEntity = CrmDataInternal.Entities.retrieveImportJobWithXML p importJob
  let status = 
    match asyncJobId with
    | None ->
      getSyncImportJobStatus importEntity
    | Some id ->
      getAsyncImportJobStatus p id
  match status with
  | Domain.ImportState.NotStarted -> (status, 0.)
  | Domain.ImportState.Succeeded -> (status, 100.)
  | _ -> (status, importEntity.GetAttributeValue<double>("progress"))

let getImportFailureMessage p importJobId aJobId = 
  match aJobId with
  | None -> 
    let importEntity = CrmDataInternal.Entities.retrieveImportJobWithXML p importJobId
    match importJobEntityHasFailure importEntity with 
    | false -> None
    | true -> 
      sprintf @"Solution import failed (ImportJob ID: %A)" importJobId 
    |> Some
  | Some id ->
      match Info.retrieveAsyncJobState p id with
      | AsyncJobState.Failed | AsyncJobState.Canceled ->
        let asyncJob = CrmData.CRUD.retrieve p "asyncoperation" id
        match asyncJob.Attributes.ContainsKey "message" with
        | true -> asyncJob.GetAttributeValue<string>("message") |> Some
        | false -> None
      | _ -> None

let printImportProgression solution pct =
  sprintf @"Import solution: %s (%i%%)" solution (pct |> int)
  |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)

let rec importLoop (proxyGen: unit -> OrganizationServiceProxy) solution importJobId asyncJobId : Async<ImportState * Option<string>> =
  async { 
    use p = proxyGen()
    let status, pct = getImportJobStatus' p importJobId asyncJobId
    printImportProgression solution pct
    match status with
    | Domain.ImportState.NotStarted
    | Domain.ImportState.InProgress ->
      do! Async.Sleep 10000 // Wait 10 seconds
      return! importLoop proxyGen solution importJobId asyncJobId
    | Domain.ImportState.Failed ->
      return Domain.ImportState.Failed, getImportFailureMessage p importJobId asyncJobId
    | Domain.ImportState.Succeeded ->
      return Domain.ImportState.Succeeded, None
    | _ -> 
      return! importLoop proxyGen solution importJobId asyncJobId
  }

let printJobResult asyncJobId importResult =
  let endStatus, _ = importResult
  let jobRes = 
    match endStatus, asyncJobId with
    | Domain.ImportState.Succeeded, Some _ -> Some @"Asynchronous import job completed"
    | Domain.ImportState.Succeeded, None -> Some @"Import job completed"
    | Domain.ImportState.Failed, Some _ -> Some @"Asynchronous import job failed"
    | Domain.ImportState.Failed, None -> Some @"Import job failed"
    | _, _ -> None
  if jobRes.IsSome then log.WriteLine(LogLevel.Verbose, jobRes.Value)

let printImportResult importJobId importResult = 
  let endStatus, err = importResult
  let importRes = 
    match endStatus, err with
    | Domain.ImportState.Succeeded, _ ->
      sprintf  @"Solution import succeeded (ImportJob ID: %A)" importJobId
    | Domain.ImportState.Failed, Some msg -> 
      sprintf "Solution import failed (ImportJob ID: %A) with message %s" importJobId msg
    | es ->
      sprintf  @"Unknown outcome of solution import (ImportJob ID: %A, End status: %A)" importJobId (Choice1Of2 es |> string)
  log.WriteLine(LogLevel.Verbose, importRes)

let getXMLResult (p: OrganizationServiceProxy) importJobId =
  let req' = new Messages.RetrieveFormattedImportJobResultsRequest()
  req'.ImportJobId <- importJobId
  let resp' = p.Execute(req') :?> Messages.RetrieveFormattedImportJobResultsResponse
  resp'.FormattedResults

let printAndSaveXMLResult excelFileToWrite (xmlContent: string) = 
  let bytes = Encoding.UTF8.GetBytes(xmlContent)
  let bytes' = SerializationHelper.xmlPrettyPrinterHelper' bytes
  let xml' = "<?xml version=\"1.0\"?>\n" + (Encoding.UTF8.GetString(bytes'))
  File.WriteAllText(excelFileToWrite, xml')
  log.WriteLine(LogLevel.Verbose, @"Import solution results saved to: " + excelFileToWrite)
