module DG.Daxif.Modules.Info.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let version org ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Retrieve CRM version:")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  logAuthentication ap usr pwd' domain log'
  let (v, _) = InfoHelper.version' org ac log'
  log'.WriteLine
    (LogLevel.Info, @"The CRM version: " + v + " was retrieved successfully")