(**
SolutionExportDev
=================

Libraries
---------

Config information *)
#load @"DG.XrmOrg.XrmSolution.Config.fsx"

module cfg = DG.XrmOrg.XrmSolution.Config

(** Open libraries for use *)
open DG.Daxif.Modules

(**
DAXIF# operations
-----------------

Export Dev *)
cfg.ensureFolder cfg.unmanaged

Solution.export
  cfg.wsdlDev' cfg.solution cfg.unmanaged false 
    cfg.authType cfg.usrDev cfg.pwdDev cfg.domainDev 
      cfg.log
