(**
SolutionUpdateCustomContext
=====================
*)

#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif

Solution.GenerateCSharpContext(cfg.devEnv, cfg.Path.xrmContext, cfg.Path.businessDomain,
  solutions = [
    cfg.solutionName
    ],
  entities = [
    // eg. "systemuser"
    ])
