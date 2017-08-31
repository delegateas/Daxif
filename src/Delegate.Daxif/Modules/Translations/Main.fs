module DG.Daxif.Modules.Translations.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let export proxyGen solution location =
  log.Verbose "Solution: %s" solution
  log.Verbose "Path to folder: %s" location
  TranslationsHelper.export' proxyGen solution location
  log.Info "The solution translations were exported successfully"
  
let import proxyGen solution location = 
  log.Verbose "Solution: %s" solution
  log.Verbose "Path to file: %s" location
  TranslationsHelper.import' proxyGen solution location
  log.Info "The solution translations were imported successfully"