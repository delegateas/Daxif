(**
SolutionImportTest
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open System.IO
open DG.Daxif

let zip = Path.Combine(cfg.Path.crmSolutions, cfg.solutionName + @".zip")

Solution.Import(cfg.devEnv, zip, activatePluginSteps = true, extended = true)
