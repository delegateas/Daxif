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
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Sync solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins VS Project: " + proj)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins Assembly: " + dll)
    logAuthentication ap usr pwd' domain log'
    PluginsHelper.syncSolution org ac solution proj dll PluginIsolationMode.Sandbox None log' 
    log'.WriteLine
      (LogLevel.Info, @"The solution plugins were synced successfully")

  let syncSolution' org solution proj dll isolationMode ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Sync solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins VS Project: " + proj)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins Assembly: " + dll)
    logAuthentication ap usr pwd' domain log'
    PluginsHelper.syncSolution org ac solution proj dll isolationMode None log'
    log'.WriteLine
      (LogLevel.Info, @"The solution plugins were synced successfully")

  let syncSolutionWhitelist org solution proj dll whitelist ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Sync solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins VS Project: " + proj)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins Assembly: " + dll)
    logAuthentication ap usr pwd' domain log'
    PluginsHelper.syncSolution org ac solution proj dll PluginIsolationMode.Sandbox (Some whitelist) log'
    log'.WriteLine
      (LogLevel.Info, @"The solution plugins were synced successfully")

  let deletePlugins org solution proj dll ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Delete solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins VS Project: " + proj)
    log'.WriteLine(LogLevel.Verbose, @"Path to Plugins Assembly: " + dll)
    logAuthentication ap usr pwd' domain log'
    PluginsHelper.deletePlugins org ac solution proj dll log'
    log'.WriteLine
      (LogLevel.Info, @"The solution plugins were deleted successfully")

  let clearPlugins org solution ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Delete solution Plugins: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    logAuthentication ap usr pwd' domain log'
    PluginsHelper.clearPlugins org ac solution log'
    log'.WriteLine
      (LogLevel.Info, @"The solution plugins were deleted successfully")