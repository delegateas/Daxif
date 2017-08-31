module internal DG.Daxif.Modules.Translations.TranslationsHelper

open System.IO
open Microsoft.Crm.Sdk
open DG.Daxif
open DG.Daxif.Common
open InternalUtility
open Microsoft.Xrm.Sdk.Client
open Microsoft.Crm.Sdk.Messages

let export' (proxyGen: unit -> OrganizationServiceProxy) solution location =
  let req = ExportTranslationRequest()
  req.SolutionName <- solution

  log.Verbose "Proxy timeout set to 1 hour"
  log.Verbose "Export translations"

  let resp = proxyGen().Execute(req) :?> ExportTranslationResponse

  log.Verbose "Translations were exported successfully"

  let zipFile = resp.ExportTranslationFile
  let filename = solution + "_Translations.zip"
  File.WriteAllBytes(location + filename, zipFile)

  log.Verbose "Solution translations saved to local disk"
  
let import' (proxyGen: unit -> OrganizationServiceProxy) solution location =
  use p = proxyGen()
  
  let zipFile = File.ReadAllBytes(location)

  log.Verbose "Translation file loaded successfully"

  let req = new ImportTranslationRequest()
  req.TranslationFile <- zipFile

  log.Verbose "Import solution"

  let resp = p.Execute(req) :?> ImportTranslationResponse // TODO: Add the % async query thingy

  log.Verbose "Solution translations were imported successfully"
  log.Verbose "Publishing solution translations"

  CrmDataHelper.publishAll p

  log.Verbose "The solution translations were successfully published"
