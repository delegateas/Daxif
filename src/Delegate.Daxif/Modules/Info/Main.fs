module DG.Daxif.Modules.Info.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let version proxyGen = 
  log.Info "Retrieve CRM version:"
  let (v, _) = InfoHelper.version' proxyGen
  log.Info "The CRM version: %s was retrieved successfully" v