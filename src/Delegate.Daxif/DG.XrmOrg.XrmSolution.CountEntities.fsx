(**
Count entities
===============

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

Count entities in solution *)
Solution.count cfg.wsdlDev' cfg.solution cfg.authType cfg.usrDev cfg.pwdDev 
  cfg.domainDev cfg.log
