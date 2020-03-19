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
  req

let executeImportSync (service: IOrganizationService) (req:ImportSolutionRequest) =
  service.Execute(req) :?> Messages.ImportSolutionResponse |> ignore
    
let executeImportAsync (service: IOrganizationService) (req:ImportSolutionRequest) = 
  let areq = new Messages.ExecuteAsyncRequest()
  areq.Request <- req
  service.Execute(areq) :?> Messages.ExecuteAsyncResponse 
  |> fun r -> r.AsyncJobId

let getImportJobStatus service (jobInfo: ImportJobInfo) =
  let jobExist = CrmDataInternal.Entities.existCrm service @"importjob" jobInfo.jobId None
  match jobExist with
  | false -> 
    (jobInfo.status, jobInfo.progress)
  | true ->
    let j = CrmDataInternal.Entities.retrieveImportJobWithXML service jobInfo.jobId
    let progress' = j.GetAttributeValue<double>("progress")

    match jobInfo.asyncJobId with
    | None ->
      let status =
        match j.Attributes.Contains("completedon") with
        | true -> ImportStatus.Completed
        | false -> jobInfo.status
      (status, progress')
    | Some id ->
      try
        match Info.retrieveAsyncJobState service id with
        | AsyncJobState.Succeeded 
        | AsyncJobState.Failed 
        | AsyncJobState.Canceled ->
        (ImportStatus.Completed, progress')
        | _ -> (jobInfo.status, progress')
      with _ -> (jobInfo.status, progress')

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
          match Info.retrieveAsyncJobState service id with
          | AsyncJobState.Succeeded -> true
          | _ -> false

        (progress' = 100.) || success
    match succeded with
    | true -> ImportResult.Success
    | false -> ImportResult.Failure
  with _ -> ImportResult.Failure

 // Check to ensure that the async job is started at all
let checkJobHasStarted service (jobInfo: ImportJobInfo) = 
  match jobInfo.asyncJobId with
  | None -> ()
  | Some id ->
    match Info.retrieveAsyncJobState service id with
    | AsyncJobState.Failed | AsyncJobState.Canceled ->
    log.WriteLine(LogLevel.Verbose, "Asynchronous import job failed")
    let systemJob = CrmData.CRUD.retrieve service "asyncoperation" id
    let msg = 
        match systemJob.Attributes.ContainsKey "message" with
        | true -> systemJob.Attributes.["message"] :?> string
        | false -> "No failure message"
    msg
    |> sprintf "Failed with message: %s"
    |> failwith 
    | _ -> ()

let printImportResult service (jobInfo: ImportJobInfo) =
  log.WriteLine(LogLevel.Verbose, @"Fetching import job result")
  match jobInfo.result with
  | Some(ImportResult.Success) -> 
    sprintf  @"Solution import succeeded (ImportJob ID: %A)" jobInfo.jobId 
    |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)
  | None
  | Some(ImportResult.Failure) ->
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
    | ImportStatus.Completed -> 
      return jobInfo
    | ImportStatus.Starting -> 
      checkJobHasStarted service jobInfo |> ignore
      let (status, pct) = getImportJobStatus service jobInfo
      let! jobInfo' = importLoop service {jobInfo with status = status; progress = pct }
      return jobInfo'
    | ImportStatus.InProgress -> 
      do! Async.Sleep 10000 // Wait 10 seconds
      let (status, pct) =
        try 
          getImportJobStatus service jobInfo
        with _ -> (ImportStatus.Completed, jobInfo.progress)
      match status with
      | ImportStatus.Starting // should not be possible
      | ImportStatus.InProgress -> 
        sprintf @"Import solution: %s (%i%%)" jobInfo.solution (pct |> int)
        |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)
        let! jobInfo' = importLoop service { jobInfo with status = status; progress = pct }
        return jobInfo'
      | ImportStatus.Completed -> 
        let result = getImportJobResult service jobInfo
        let jobInfo' = { jobInfo with status = status; progress = pct; result = Some(result) }
        return jobInfo'
  }

let saveImportJobResult (service: IOrganizationService) jobResult (excelLocation:string) = 
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

let import (env: Environment) solution location managed = 
  let service = env.connect().GetService()
  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

  let zipFile = File.ReadAllBytes(location)
  log.WriteLine(LogLevel.Verbose, @"Solution file loaded successfully")

  let jobId = Guid.NewGuid()
  let req = createImportRequest zipFile jobId managed

  log.WriteLine(LogLevel.Verbose, @"Proxy timeout set to 1 hour")
    
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
        status = ImportStatus.Starting
        progress = 0.
        result = None
        excelFile = None
      }
      let! progress = importLoop service impData
      return progress
    }
      
  let jobResult = 
    log.WriteLine(LogLevel.Debug,"Starting Import Job - check 0")
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
  log.WriteLine(LogLevel.Verbose, @"Fetching import job result")
  generateExcelResultLocation  location
  |> saveImportJobResult service jobResult