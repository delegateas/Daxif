module internal DG.Daxif.Modules.Info.InfoHelper

open DG.Daxif
open DG.Daxif.Common

let version' org ac (log : ConsoleLogger) = 
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  CrmDataInternal.Info.version p
