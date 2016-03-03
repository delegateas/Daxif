(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

/// Implements function which allow you to synchronize workflows in a solution
module Workflow = 
(**
Workflow
========

*)
  /// Syncronize workflow with a solution
  val public syncSolution : wsdl:Uri
     -> solution:string
     -> dll:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
