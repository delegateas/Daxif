namespace DG.Daxif.Modules

open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility

module Plugins = 
  let syncSolution org solution proj dll ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Sync solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins VS Project: " + proj)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins Assembly: " + dll)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      PluginsHelper.syncSolution' org ac solution proj dll log'
      log'.WriteLine
        (LogLevel.Info, @"The solution Plugins were synced successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)


   let cleanTarget org solution apSrc usrSrc pwdSrc domainSrc apTar usrTar pwdTar domainTar log =
    let acSrc = Authentication.getCredentials apSrc usrSrc pwdSrc domainSrc
    let pwdSrc' = String.replicate pwdSrc.Length "*"
    let acTar = Authentication.getCredentials apTar usrTar pwdTar domainTar
    let pwdTar' = String.replicate pwdTar.Length "*"
    let log' = ConsoleLogger.ConsoleLogger log
    log'.WriteLine(LogLevel.Info, @"Sync solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider for Source: " + apSrc.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usrSrc)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwdSrc')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domainSrc)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider for Target: " + apTar.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usrTar)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwdTar')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domainTar)
    try 
      PluginsHelper.cleanTarget' org acSrc acTar solution log'
      log'.WriteLine
        (LogLevel.Info, @"The target Plugins were cleaned compared to source")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
    

