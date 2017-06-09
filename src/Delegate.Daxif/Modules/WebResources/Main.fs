module DG.Daxif.Modules.WebResource.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let syncSolution org solution webresourceRoot ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Sync solution webresources: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to webresources: " + webresourceRoot)
  logAuthentication ap usr pwd' domain log'
  WebResourcesHelper.syncSolution' org ac webresourceRoot log'
  log'.WriteLine
    (LogLevel.Info, @"The solution webresources were synced successfully")