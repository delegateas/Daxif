(**
Count entities
===============
*)

#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif

Solution.Count(cfg.devEnv, cfg.solutionName)
