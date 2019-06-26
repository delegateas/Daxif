module DG.Daxif.Modules.Workflow.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let syncSolution proxyGen solution dll isolationMode = 
  log.Info @"Sync solution Workflow: %s" solution
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to Workflow Assembly: %s" dll
  WorkflowsHelper.syncSolution' proxyGen solution dll isolationMode
  log.Info @"The solution Workflow were synced successfully"
