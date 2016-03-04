(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

/// Implements function which allow you to synchronize plugins in a solution
module Plugins = 
(**
Plugins
==================

*)
  /// Synchronize plugins in a solution
  val public syncSolution : wsdl:Uri
     -> solution:string
     -> proj:string
     -> dll:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
