module DG.Daxif.Modules.WebResource.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let syncSolution proxyGen solution webresourceRoot patchSolutionName = 
  logVersion log
  let patchInfo = match patchSolutionName with
                  | Some s -> sprintf ". Created/updated web resources will be added to: %s" s
                  | None -> ""
  log.Info @"Sync solution webresources: %s%s" solution patchInfo
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to webresources: %s" webresourceRoot
  WebResourcesHelper.syncSolution proxyGen webresourceRoot solution patchSolutionName
  log.Info "The solution webresources were synced successfully"