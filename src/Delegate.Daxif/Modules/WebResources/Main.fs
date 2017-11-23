module DG.Daxif.Modules.WebResource.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let syncSolution proxyGen solution webresourceRoot wrPrefix= 
  logVersion log
  log.Info @"Sync solution webresources: %s" solution
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to webresources: %s" webresourceRoot
  WebResourcesHelper.syncSolution proxyGen webresourceRoot solution wrPrefix
  log.Info @"The solution webresources were synced successfully"