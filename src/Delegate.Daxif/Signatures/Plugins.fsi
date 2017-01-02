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

  /// Synchronize plugins in a solution
  val public syncSolution' : wsdl:Uri
     -> solution:string
     -> proj:string
     -> dll:string
     -> isolationMode:PluginIsolationMode
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit

  /// Delete plugins in target solution that does not exist in source dll
  val public deletePlugins : wsdl:Uri
     -> solution:string
     -> proj:string
     -> dll:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
