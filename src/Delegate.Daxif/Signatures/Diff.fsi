(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open Suave

/// Contains several ways of seeing the difference between two packaged Crm solutions
module Diff = 
(**
Diff
==========

*)
  /// Interactive service for exploring the difference between two package solutions. Starts a local webserver and opens the default browser.
  val public solution : source:string
     -> target:string -> logLevel:LogLevel -> unit
  /// Returns a webpart used by Suave.io to display the difference between two packaged solutions.
  val public solutionApp : source:string
     -> target:string -> logLevel:LogLevel -> WebPart
  /// Creates a csv file containing the difference between two packaged files.
  val public summary : source:string
     -> target:string -> logLevel:LogLevel -> string
