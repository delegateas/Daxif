(**
Config
======

Sets up all the necessary variables and functions to be used for the other
scripts. 
*)
#r @"Microsoft.Xrm.Sdk.dll"
#r @"Delegate.Daxif.dll"
open System
open System.IO
open DG.Daxif
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__


let (++) x1 x2 = Path.GetFullPath(Path.Combine(x1, x2))

let daxifRoot = __SOURCE_DIRECTORY__
