(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

/// Implements functions which allows you to export and import state of entities
module State = 
(**
State
==============

Export and import the state of views, plugins and workflows and store them in an existing 
exported solution. To import the states require that the states were also exported 
with the State export function.
*)
  /// Creates a new publisher with the given parameters.
  val public exportStates : wsdl:Uri
     -> solution:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> path:string 
     -> logLevel:LogLevel -> unit
