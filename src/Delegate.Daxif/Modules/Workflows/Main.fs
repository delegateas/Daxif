module DG.Daxif.Modules.Workflow.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let syncSolution org solution dll ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Sync solution Workflow: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to Workflow Assembly: " + dll)
  logAuthentication ap usr pwd' domain log'
  WorkflowsHelper.syncSolution' org ac solution dll log'
  log'.WriteLine
    (LogLevel.Info, @"The solution Workflow were synced successfully")