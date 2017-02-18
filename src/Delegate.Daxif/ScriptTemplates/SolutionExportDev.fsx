(**
SolutionExportDev
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open System.IO
open DG.Daxif

// Export unmanaged
Solution.Export(cfg.devEnv, cfg.solutionName, cfg.Path.crmSolutions, managed = false, extended = true);;

// Export managed
Solution.Export(cfg.devEnv, cfg.solutionName, cfg.Path.crmSolutions, managed = true, extended = true)
