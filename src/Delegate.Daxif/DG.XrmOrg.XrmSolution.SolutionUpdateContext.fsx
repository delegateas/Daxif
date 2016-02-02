(**
SolutionUpdateContext
=====================

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

Generate System Context *)
let csu = cfg.tools + @"\MS\CrmSvcUtil\CrmSvcUtil.exe"
let ctx = cfg.rootFolder + @"\..\..\BusinessDomain"

Solution.updateServiceContext
  cfg.wsdlDev' ctx
    cfg.authType cfg.usrDev cfg.pwdDev cfg.domainDev
      csu None (* or: "1030 |> Some" for Danish labels *) cfg.log
