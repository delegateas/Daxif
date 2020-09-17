module internal DG.Daxif.Modules.Solution.Export

open System
open System.IO
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open Microsoft.Crm.Sdk.Messages
open Domain
open AsyncJobHelper


let createExportRequest solutionName managed = 
  let reqId = Guid.NewGuid()
  let req = new Messages.ExportSolutionRequest()
  req.Managed <- managed
  req.SolutionName <- solutionName
  req.RequestId <- new Nullable<Guid>(reqId)
  req

let createExportAsyncRequest solutionName managed = 
  let req = createExportRequest solutionName managed
  req.RequestName <- "ExportSolutionAsync"
  req

let writeSolutionFile solutionName managed location bytes =
  let filePath = location ++ (CrmUtility.generateSolutioZipFilename solutionName managed)
  File.WriteAllBytes(filePath, bytes)
  log.Verbose @"Solution saved to local disk (%s)" filePath
  filePath

let retrieveFileByte (service: IOrganizationService) exportJobId =
  let solReq = OrganizationRequest("DownloadSolutionExportData");
  solReq.Parameters.Add("ExportJobId", exportJobId)
  let solresp = service.Execute(solReq)
  solresp.Results.["ExportSolutionFile"] :?> byte[]

let executeExportAsync (service: IOrganizationService) (req:ExportSolutionRequest) = 
  let resp = service.Execute(req)
  let asyncJobId = resp.Results.["AsyncOperationId"] :?> Guid
  let exportJobId = resp.Results.["ExportJobId"] :?> Guid
  asyncJobId, exportJobId

let rec exportLoop service (jobInfo: ExportAsyncJobInfo) = 
  async { 
    match jobInfo.status with
    | AsyncJobStatus.Completed -> 
      return jobInfo
    | AsyncJobStatus.Starting -> 
      AsyncJobHelper.checkJobHasStarted service jobInfo.asyncJobId |> ignore
      let status = AsyncJobHelper.getJobStatus service jobInfo.asyncJobId
      let! jobInfo' = exportLoop service {jobInfo with status = status }
      return jobInfo'
    | AsyncJobStatus.InProgress -> 
      do! Async.Sleep 10000 // Wait 10 seconds
      let status =
        try 
          AsyncJobHelper.getJobStatus service jobInfo.asyncJobId
        with _ -> AsyncJobStatus.Completed
      match status with
      | AsyncJobStatus.Starting // should not be possible
      | AsyncJobStatus.InProgress -> 
        sprintf @"Export solution: In Progress"
        |> fun msg -> log.WriteLine(LogLevel.Verbose, msg)
        let! jobInfo' = exportLoop service { jobInfo with status = status}
        return jobInfo'
      | AsyncJobStatus.Completed -> 
        let result = getJobResult service jobInfo.asyncJobId
        let jobInfo' = { jobInfo with status = status; result = Some(result) }
        return jobInfo'
  }

let exportSync (service: IOrganizationService) solution location managed = 
  log.Info @"Exporting solution"
  let req = createExportRequest solution managed
  log.Debug @"Execution export request (RequestId: %A)" req.RequestId
  let resp = service.Execute(req) :?> ExportSolutionResponse

  let fileBytes = resp.ExportSolutionFile
  let filePath = writeSolutionFile solution managed location fileBytes
  log.Info @"Solution exported successfully"
  filePath

let exportAsync (service: IOrganizationService) solution location managed = 
  log.WriteLine(LogLevel.Info, @"Exporting solution")
  
  let req = createExportAsyncRequest solution managed
    
  let startExport () = 
    async { 

      let (asyncJobId, exportJobId) = executeExportAsync service req
      log.WriteLine(LogLevel.Verbose,@"Asynchronous export job started")
  
      let exportData = {
        solution = solution
        managed = managed
        jobId = exportJobId
        asyncJobId = asyncJobId
        status = AsyncJobStatus.Starting
        result = None
      }
      let! progress = exportLoop service exportData
      return progress
    }
      
  let jobResult = 
    startExport()
    |> Async.Catch
    |> Async.RunSynchronously
    |> function
      | Choice2Of2 exn ->
        log.WriteLine(LogLevel.Error, exn.Message)
        raise exn
      | Choice1Of2 jobResult ->
        jobResult
  
  let filePath =
    retrieveFileByte service jobResult.jobId
    |> writeSolutionFile solution managed location

  log.Info @"Solution exported successfully"

  filePath
