module DG.Daxif.Modules.Solution.Extend

open System
open System.IO
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let extendSolution (env: Environment) solution location managed = 
  logVersion log
  log.Info @"Extend solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  log.Verbose @"Managed solution: %O" managed
  env.logAuthentication log
  log.WriteLine(LogLevel.Info, @"Exporting extended solution")
  ExtendedSolutionHelper.exportExtendedSolution env solution location managed
  log.Info @"The solution was extended successfully"

let preImportExtendedSolution (env: Environment) solution location managed = 
  logVersion log
  log.Info @"Pre-import extend solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  env.logAuthentication log
  log.WriteLine(LogLevel.Info, @"Preforming pre steps of extended solution")
  ExtendedSolutionHelper.preImportExtendedSolution env solution location managed
  log.Info @"Pre-import of extended solution completed"

let postImportExtendedSolution (env: Environment) solution location managed = 
  logVersion log
  log.Info @"Post-import extend solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  env.logAuthentication log
  log.WriteLine(LogLevel.Info, @"Preforming pre steps of extended solution")
  ExtendedSolutionHelper.postImportExtendedSolution env solution location managed
  log.Info @"Post-import of extended solution completed"

let exportWithExtendedSolution (env: Environment) solution location managed = 
  logVersion log
  log.Info @"Export solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  log.Verbose @"Managed solution: %O" managed
  env.logAuthentication log
  SolutionHelper.exportWithExtendedSolution' env solution location managed
  log.Info @"The extended solution was exported successfully"

let importWithExtendedSolution (env: Environment) location = 
  let solution, managed = CrmUtility.getSolutionInformationFromFile location
  logVersion log
  log.Info @"Import solution: %s" solution
  log.Verbose @"Organization: %O" env.url
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" location
  log.Verbose @"Managed solution: %O" managed
  env.logAuthentication log
  SolutionHelper.importWithExtendedSolution' env solution location managed |> ignore
  log.Info @"The extended solution was imported successfully"

