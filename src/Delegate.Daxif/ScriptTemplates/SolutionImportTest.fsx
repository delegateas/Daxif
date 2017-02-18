(**
SolutionImportTest
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open System.IO
open DG.Daxif

let zip = Path.Combine(cfg.Path.crmSolutions, cfg.solutionName + @"_managed_.zip")

Solution.Import(cfg.testEnv, zip, activatePluginSteps = true, extended = true)
