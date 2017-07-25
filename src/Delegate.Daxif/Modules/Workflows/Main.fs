module DG.Daxif.Modules.Workflow.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let syncSolution org solution dll ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log

  log.Info @"Sync solution Workflow: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to Workflow Assembly: %s" dll
  logAuthentication ap usr pwd' domain log
  WorkflowsHelper.syncSolution' org ac solution dll log
  log.Info @"The solution Workflow were synced successfully"