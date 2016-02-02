namespace DG.Daxif.HelperModules

open System.IO
open Microsoft.Crm.Sdk
open DG.Daxif
open DG.Daxif.HelperModules.Common

module internal TranslationsHelper = 
  let export' org ac solution location (log : ConsoleLogger.ConsoleLogger) = 
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc
    let req = new Messages.ExportTranslationRequest()

    log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
    log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

    req.SolutionName <- solution

    log.WriteLine(LogLevel.Verbose, @"Proxy timeout set to 1 hour")
    log.WriteLine(LogLevel.Verbose, @"Export translations")

    let resp = p.Execute(req) :?> Messages.ExportTranslationResponse

    log.WriteLine(LogLevel.Verbose, @"Translations were exported successfully")

    let zipFile = resp.ExportTranslationFile
    let filename = solution + "_Translations.zip"
    File.WriteAllBytes(location + filename, zipFile)

    log.WriteLine
      (LogLevel.Verbose, @"Solution translations saved to local disk")
  
  let import' org ac solution location (log : ConsoleLogger.ConsoleLogger) = 
    let (solution_ : string) = solution
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc

    log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
    log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")

    let zipFile = File.ReadAllBytes(location)

    log.WriteLine(LogLevel.Verbose, @"Translation file loaded successfully")

    let req = new Messages.ImportTranslationRequest()

    req.TranslationFile <- zipFile
    log.WriteLine(LogLevel.Verbose, @"Proxy timeout set to 1 hour")
    log.WriteLine(LogLevel.Verbose, @"Import solution")

    let resp = p.Execute(req) :?> Messages.ImportTranslationResponse // TODO: Add the % async query thingy

    log.WriteLine
      (LogLevel.Verbose, @"Solution translations were imported successfully")
    log.WriteLine(LogLevel.Verbose, @"Publishing solution translations")

    CrmData.CRUD.publish p

    log.WriteLine
      (LogLevel.Verbose, 
       @"The solution translations were successfully published")
