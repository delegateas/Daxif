(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

/// Implements functions to get information about the CRM
module Info = 
  (**
  Info
  ==========

  Retrieves and outputs the CRM version to the log.
  *)
  /// Retrieves and outputs the CRM version to the log.
  val public version : wsdl:Uri
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
