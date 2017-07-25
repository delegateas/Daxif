module DG.Daxif.Modules.WebResource.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let syncSolution org solution webresourceRoot ap usr pwd domain = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let pwd' = String.replicate pwd.Length "*"

  logVersion log
  log.Info @"Sync solution webresources: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to webresources: %s" webresourceRoot
  logAuthentication ap usr pwd' domain log
  WebResourcesHelper.syncSolution org ac webresourceRoot solution
  log.Info @"The solution webresources were synced successfully"