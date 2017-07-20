﻿module DG.Daxif.Modules.Translations.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let export org solution location ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Export solution translations: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  logAuthentication ap usr pwd' domain log'
  TranslationsHelper.export' org ac solution location log'
  log'.WriteLine
    (LogLevel.Info, @"The solution translations were exported successfully")
  
let import org solution location ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Import solution translations: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
  logAuthentication ap usr pwd' domain log'
  TranslationsHelper.import' org ac solution location log'
  log'.WriteLine
    (LogLevel.Info, @"The solution translations were imported successfully")