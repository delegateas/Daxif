module internal DG.Daxif.Modules.Solution.Export

open System
open System.IO
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility

let execute (service: IOrganizationService) solution location managed (log : ConsoleLogger) = 
  log.Info @"Exporting solution"
  
  let reqId = Guid.NewGuid()
  let req = new Messages.ExportSolutionRequest()
  req.Managed <- managed
  req.SolutionName <- solution
  req.RequestId <- new Nullable<Guid>(reqId)

  log.Debug @"Execution export request (RequestId: %A)" reqId
  let resp = service.Execute(req) :?> Messages.ExportSolutionResponse

  let zipFile = resp.ExportSolutionFile
  let filePath = location ++ CrmUtility.generateSolutioZipFilename solution managed
  File.WriteAllBytes(filePath, zipFile)
  log.Verbose @"Solution saved to local disk (%s)" filePath
  log.Info @"Solution exported successfully"
  filePath
