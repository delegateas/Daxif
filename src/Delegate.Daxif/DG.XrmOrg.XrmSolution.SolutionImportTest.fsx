(**
SolutionImportTest
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
  cfg.wsdlTest' cfg.solution zip false
    cfg.authType cfg.usrTest cfg.pwdTest cfg.domainTest 
      cfg.log
