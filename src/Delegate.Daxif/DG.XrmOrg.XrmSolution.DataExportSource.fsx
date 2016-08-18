(**
DataExportSource
=================

Libraries
---------

Config information *)
#load @"DG.XrmOrg.XrmSolution.Config.fsx"

module cfg = DG.XrmOrg.XrmSolution.Config

(** Open libraries for use *)

open DG.Daxif
open DG.Daxif.Modules

type source = XrmProvider<uri = cfg.wsdlSource, usr = cfg.usrSource, 
                          pwd = cfg.pwdSource, domain = cfg.domainSource,
                          ap = cfg.authType>

(**
DAXIF# operations
---------

Define entities to be exported *)

let entities = 
  [|
    source.Metadata.Account.``(LogicalName)``;
    source.Metadata.Contact.``(LogicalName)``
  |]

(** Export entities and store them as xml at data location *)

Data.export
  cfg.wsdlSource' cfg.data entities 
    cfg.authType cfg.usrSource cfg.pwdSource
      cfg.domainSource LogLevel.Debug 
        Serialize.XML