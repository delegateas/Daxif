module internal DG.Daxif.Modules.Solution.Import

open System
open System.IO
open System.Text
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Common.CrmDataInternal
open DG.Daxif.Modules.Serialization
open Microsoft.Crm.Sdk.Messages
open Domain
open AsyncJobHelper

let generateExcelResultLocation (solutionLocation: string) =
  let location' = solutionLocation.Replace(@".zip", "")
  location' + @"_" + Utility.timeStamp'() + @".xml"

let createImportRequest zipFile jobId managed =
  let req = new ImportSolutionRequest()
  req.CustomizationFile <- zipFile
  req.ImportJobId <- jobId
  req.ConvertToManaged <- managed
  req.OverwriteUnmanagedCustomizations <- true
  req.PublishWorkflows <- true
  req.RequestId <- new Nullable<Guid>(Guid.NewGuid())
  req

let executeImportSync (service: IOrganizationService) (req:ImportSolutionRequest) =
  log.Debug @"Execution import request (RequestId: %A)" req.RequestId.Value
  service.Execute(req) :?> Messages.ImportSolutionResponse |> ignore
    
let executeImportAsync (service: IOrganizationService) (req:ImportSolutionRequest) = 
  let areq = new Messages.ExecuteAsyncRequest()
  areq.Request <- req
  let asyncJobId = 
    service.Execute(areq) :?> Messages.ExecuteAsyncResponse 
    |> fun r -> r.AsyncJobId
  log.Debug @"Execution import request asyncrunously (AsyncJobId: %A, RequestId: %A)" asyncJobId req.RequestId.Value
  asyncJobId

let getImportJobStatus service (jobInfo: ImportJobInfo) =
  let jobExist = CrmDataInternal.Entities.existCrm service @"importjob" jobInfo.jobId None
  match jobExist with
  | false -> 
    (AsyncJobStatus.Starting, jobInfo.progress)
  | true ->
    let j = CrmDataInternal.Entities.retrieveImportJobWithXML service jobInfo.jobId
    let progress' = j.GetAttributeValue<double>("progress")

    match jobInfo.asyncJobId with
    | None ->
      let status =
        match j.Attributes.Contains("completedon") with
        | true -> AsyncJobStatus.Completed
        | false -> AsyncJobStatus.InProgress
      (status, progress')
    | Some id ->
      try
        match AsyncJobHelper.retrieveAsyncJobState service id with
        | AsyncJobState.Succeeded 
        | AsyncJobState.Failed 
        | AsyncJobState.Canceled -> (AsyncJobStatus.Completed, progress')
        | _ -> (AsyncJobStatus.InProgress, progress')
      with _ -> (AsyncJobStatus.InProgress, progress')

// Check to ensure that the async job is started at all
let checkJobHasStarted service (jobInfo: ImportJobInfo) = 
  match jobInfo.asyncJobId with
  | None -> ()
  | Some id ->
    AsyncJobHelper.checkJobHasStarted service id

let getImportJobResult service (jobInfo: ImportJobInfo) =
  try
    let j = CrmDataInternal.Entities.retrieveImportJobWithXML service jobInfo.jobId
    let progress' = j.Attributes.["progress"] :?> double
    let succeded =
      match jobInfo.asyncJobId with
      | None -> 
        log.WriteLine(LogLevel.Verbose,@"Import job completed")
        let data = j.Attributes.["data"] :?> string
        let success = not (data.Contains("<result result=\"failure\""))

        (progress' = 100.) || success
      | Some id -> 
        log.WriteLine(LogLevel.Verbose,@"Asynchronous import job completed")
        let success = 
          match AsyncJobHelper.retrieveAsyncJobState service id with
          | AsyncJobState.Succeeded -> true
          | _ -> false

        (progress' = 100.) || success
    match succeded with
    | true -> JobResult.Success
    | false -> JobResult.Failure
  with _ -> JobResult.Failure

let printImportResult service (jobInfo: ImportJobInfo) =
  match jobInfo.result with
  | Some(JobResult.Success) -> 
    sprintf  @"Solution was imported successfully (ImportJob ID: %A)" jobInfo.jobId 
    |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)
  | None
  | Some(JobResult.Failure) ->
    let msg =
      match jobInfo.asyncJobId with
      | None -> 
        sprintf @"Solution import failed (ImportJob ID: %A)" jobInfo.jobId
      | Some(id) ->
        let systemJob = CrmData.CRUD.retrieve service "asyncoperation" id
        let msg = 
          match systemJob.Attributes.ContainsKey "message" with
          | true -> systemJob.Attributes.["message"] :?> string
          | false -> "No failure message"
        sprintf "Solution import failed (ImportJob ID: %A) with message %s" jobInfo.jobId msg
    failwith msg

let rec importLoop service (jobInfo: ImportJobInfo) = 
  async { 
    match jobInfo.status with
    | AsyncJobStatus.Completed -> 
      return jobInfo
    | AsyncJobStatus.Starting -> 
      checkJobHasStarted service jobInfo |> ignore
      let (status, pct) = getImportJobStatus service jobInfo
      let! jobInfo' = importLoop service {jobInfo with status = status; progress = pct }
      return jobInfo'
    | AsyncJobStatus.InProgress -> 
      do! Async.Sleep 10000 // Wait 10 seconds
      let (status, pct) =
        try 
          getImportJobStatus service jobInfo
        with _ -> (AsyncJobStatus.Completed, jobInfo.progress)
      match status with
      | AsyncJobStatus.Starting // should not be possible
      | AsyncJobStatus.InProgress -> 
        sprintf @"Import solution: %s (%i%%)" jobInfo.solution (pct |> int)
        |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)
        let! jobInfo' = importLoop service { jobInfo with status = status; progress = pct }
        return jobInfo'
      | AsyncJobStatus.Completed -> 
        let result = getImportJobResult service jobInfo
        let jobInfo' = { jobInfo with status = status; progress = pct; result = Some(result) }
        return jobInfo'
  }

let saveImportJobResult (service: IOrganizationService) (jobResult:ImportJobInfo) (excelLocation:string) = 
  log.WriteLine(LogLevel.Verbose, @"Fetching import job result")
  try  
    let req' = new Messages.RetrieveFormattedImportJobResultsRequest()
    req'.ImportJobId <- jobResult.jobId
    let resp' = 
      service.Execute(req') :?> Messages.RetrieveFormattedImportJobResultsResponse
    let xml = resp'.FormattedResults
    let bytes = Encoding.UTF8.GetBytes(xml)
    let bytes' = SerializationHelper.xmlPrettyPrinterHelper' bytes
    let xml' = "<?xml version=\"1.0\"?>\n" + (Encoding.UTF8.GetString(bytes'))
    File.WriteAllText(excelLocation, xml')
    log.WriteLine(LogLevel.Verbose, @"Import solution results saved to: " + excelLocation)
    { jobResult with excelFile = Some(excelLocation) }
  with 
  | ex -> 
    log.WriteLine(LogLevel.Error, ex.Message)
    raise ex

let publish service managed =
  if not managed then
    log.WriteLine(LogLevel.Verbose, @"Publishing customizations")
    CrmDataHelper.publishAll service
    log.WriteLine(LogLevel.Verbose, @"The solution was successfully published")

let execute service solution location managed = 
  log.WriteLine(LogLevel.Info, @"Importing solution")
  let zipFile = File.ReadAllBytes(location)
  log.WriteLine(LogLevel.Verbose, @"Solution file loaded successfully")

  let jobId = Guid.NewGuid()
  let req = createImportRequest zipFile jobId managed
    
  let startImport () = 
    async { 
      let asyncJobId = 
        match CrmDataInternal.Info.version service with
        | (_, CrmReleases.CRM2011) -> 
          executeImportSync service req
          log.WriteLine(LogLevel.Verbose,@"Import job Started")
          None
        | (_, _) -> 
          let asyncJobId = executeImportAsync service req
          log.WriteLine(LogLevel.Verbose,@"Asynchronous import job started")
          Some (asyncJobId)

      log.WriteLine(LogLevel.Verbose, @"Import solution: " + solution + @" (0%)")
      let impData = {
        solution = solution
        managed = managed
        jobId = jobId
        asyncJobId = asyncJobId
        status = AsyncJobStatus.Starting
        progress = 0.
        result = None
        excelFile = None
      }
      let! progress = importLoop service impData
      return progress
    }
      
  let jobResult = 
    startImport()
    |> Async.Catch
    |> Async.RunSynchronously
    |> function
      | Choice2Of2 exn ->
        log.WriteLine(LogLevel.Error, exn.Message)
        raise exn
      | Choice1Of2 jobResult ->
        jobResult

  printImportResult service jobResult

  // Save the XML file
  generateExcelResultLocation  location
  |> saveImportJobResult service jobResult