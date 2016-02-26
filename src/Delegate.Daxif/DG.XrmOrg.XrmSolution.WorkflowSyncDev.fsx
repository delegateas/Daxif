(**
WebResouresSyncDev
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

Sync Plugins Dev *)
let dll  = cfg.rootFolder + @"\..\Workflow\bin\Release\ILMerged.Delegate.XrmOrg.XrmSolution.Plugins.dll"

Workflow.syncSolution
  cfg.wsdlDev' cfg.solution dll
    cfg.authType cfg.usrDev cfg.pwdDev cfg.domainDev 
      cfg.log
