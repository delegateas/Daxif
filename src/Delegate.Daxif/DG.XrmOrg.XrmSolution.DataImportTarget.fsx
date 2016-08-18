(**
DataImportTarget
=================

Libraries
---------

Config information *)
#load @"DG.XrmOrg.XrmSolution.Config.fsx"

module cfg = DG.XrmOrg.XrmSolution.Config

(** Open libraries for use *)
open DG.Daxif
open DG.Daxif.Modules

(**
DAXIF# operations
---------

Import the entities from the xml files in the data location *)

Data.import 
  cfg.wsdlTarget' cfg.data cfg.authType
    cfg.usrTarget cfg.pwdTarget cfg.domainTarget
      LogLevel.Debug Serialize.XML 
        Map.empty Map.empty