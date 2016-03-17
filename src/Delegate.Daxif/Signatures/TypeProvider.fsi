(*** hide ***)
namespace DG.Daxif.Modules.TypeProvider

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes

(**
TypeProvider
==================

In order to provide the user with type safe values, based on their solution, they
can provide as input parameters for the modules contained in DAXIF# we provide
a basic and readonly xRM F# TypeProvider that is able to retrieve data/metadata 
from a given MS CRM instance, and provide it as Intellisense in Visual Studio. 
*)

/// Basic and simple/read-only F# xRM TypeProvider
[<CompilerServices.TypeProviderAttribute ()>]
type XrmProvider = 
  inherit TypeProviderForNamespaces
  new : config:TypeProviderConfig -> XrmProvider
