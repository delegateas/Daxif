(**
SolutionUpdateTsContext
=====================
*)

#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif

Solution.GenerateTypeScriptContext(cfg.devEnv, cfg.Path.xrmDefinitelyTyped, cfg.Path.xrmTypings,
  solutions = [
    cfg.solutionName
    ],
  entities = [
    // eg. "systemuser"
    ],
  extraArguments = [
    "web", ""
    "jsLib", cfg.Path.jsLib
    ])