namespace DG.Daxif.Modules

open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility

module State =
  let exportStates org solution ap usr pwd domain path log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Sync solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      StateHelper.exportStates' org ac solution path log'
      log'.WriteLine
        (LogLevel.Info, @"The solution Plugins were synced successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)

  

