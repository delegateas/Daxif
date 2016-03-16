namespace DG.Daxif.HelperModules

open DG.Daxif.HelperModules.Common

module internal InfoHelper = 
  let version' org ac (log : ConsoleLogger.ConsoleLogger) = 
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc
    CrmDataInternal.Info.version p
