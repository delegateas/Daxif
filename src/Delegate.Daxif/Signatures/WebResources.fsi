(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

module WebResources = 
  (**
  WebResources
  ==================

  *)
  /// TODO:
  val public syncSolution : wsdl:Uri
     -> solution:string
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
