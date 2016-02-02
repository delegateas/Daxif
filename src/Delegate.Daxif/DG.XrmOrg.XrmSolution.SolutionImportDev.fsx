(**
SolutionImportDev
=================

Libraries
---------

Config information *)
#load @"DG.XrmOrg.XrmSolution.Config.fsx"

module cfg = DG.XrmOrg.XrmSolution.Config

(** Open libraries for use *)
open DG.Daxif.Modules

module cfg = DG.XrmOrg.XrmSolution.Config

(**
DAXIF# operations
-----------------

Import Dev *)
let zip = cfg.unmanaged + cfg.solution + @".zip"

Solution.import
  cfg.wsdlDev' cfg.solution zip false
    cfg.authType cfg.usrDev cfg.pwdDev cfg.domainDev 
      cfg.log
