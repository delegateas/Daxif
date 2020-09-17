module DG.Daxif.Modules.Solution.AsyncJobHelper

open System
open System.IO
open System.Text
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Common.CrmDataHelper

type JobResult = 
  | Failure
  | Success

type AsyncJobStatus = 
  | Starting
  | InProgress 
  | Completed 

let retrieveAsyncJobState proxy asyncJobId =
  let systemJob = CrmDataHelper.retrieve proxy "asyncoperation" asyncJobId (RetrieveSelect.Fields ["statuscode"])
  systemJob.GetAttributeValue<OptionSetValue>("statuscode")
  |> fun o -> Utility.stringToEnum<AsyncJobState> (o.Value.ToString())

let getJobStatus service asyncJobId =
  try
    match retrieveAsyncJobState service asyncJobId with
    | AsyncJobState.Succeeded | AsyncJobState.Failed | AsyncJobState.Canceled -> AsyncJobStatus.Completed
    | _ -> AsyncJobStatus.InProgress
  with _ -> AsyncJobStatus.InProgress

let getJobResult service asyncJobId =
  try
    log.WriteLine(LogLevel.Verbose,@"Asynchronous job completed")
    match retrieveAsyncJobState service asyncJobId with
    | AsyncJobState.Succeeded -> JobResult.Success
    | _ -> JobResult.Failure
  with _ -> JobResult.Failure

// Check to ensure that the async job is started at all
let checkJobHasStarted service asyncJobId = 
  match retrieveAsyncJobState service asyncJobId with
  | AsyncJobState.Failed | AsyncJobState.Canceled ->
  log.WriteLine(LogLevel.Verbose, "Asynchronous job failed")
  let systemJob = CrmData.CRUD.retrieve service "asyncoperation" asyncJobId
  let msg = 
      match systemJob.Attributes.ContainsKey "message" with
      | true -> systemJob.Attributes.["message"] :?> string
      | false -> "No failure message"
  msg
  |> sprintf "Failed with message: %s"
  |> failwith 
  | _ -> ()


