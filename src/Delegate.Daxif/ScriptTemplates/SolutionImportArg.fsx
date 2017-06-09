(**
SolutionImportArg
=================

Should be run with the FSI commandline and given an "env=<env-name>" argument.
Performs a solution import to the environment which matches the given env-name.

Add "managed" as an argument to import the managed solution instead of the unmanaged.
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let args = fsi.CommandLineArgs |> parseArgs

let env =
  match args |> tryFindArg ["env"; "e"] with
  | Some arg -> Environment.Get arg
  | None     -> failwithf "Missing 'env' argument needed to execute this script."

let solutionZipPath = 
  match args |> tryFindArg ["managed"] with
  | Some _ -> "_managed"
  | None   -> ""
  |> sprintf "%s%s.zip" XrmSolution.name
  |> (++) Path.Daxif.crmSolutionsFolder


Solution.Import(env, solutionZipPath, activatePluginSteps = true, extended = true)
