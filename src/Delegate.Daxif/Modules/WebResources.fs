namespace DG.Daxif.Modules

open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility

module WebResources = 
  let syncSolution org solution location ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Sync solution webresources: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to webresources: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    WebResourcesHelper.syncSolution' org ac location log'
    log'.WriteLine
      (LogLevel.Info, @"The solution webresources were synced successfully")