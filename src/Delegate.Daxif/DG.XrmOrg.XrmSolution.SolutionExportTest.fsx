(**
SolutionExportTest
==================

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

Export Test *)
cfg.ensureFolder cfg.unmanaged

Solution.export
  cfg.wsdlTest' cfg.solution cfg.unmanaged false 
    cfg.authType cfg.usrTest cfg.pwdTest cfg.domainTest 
      cfg.log
