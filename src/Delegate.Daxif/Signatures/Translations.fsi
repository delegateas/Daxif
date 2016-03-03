(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

module Translations = 
(**
Translations
==================

*)
  /// TODO:
  val public export : wsdl:Uri
     -> solution:string
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  /// TODO:
  val public import : wsdl:Uri
     -> solution:string
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
