(**
SolutionCreateDev
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open System.IO
open DG.Daxif

Solution.CreatePublisher(cfg.devEnv, cfg.pubName, cfg.pubDisplay, cfg.pubPrefix)

Solution.Create(cfg.devEnv, cfg.solutionName, cfg.solutionDisplayName, cfg.pubPrefix)
