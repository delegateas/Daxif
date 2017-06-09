(**
SolutionUpdateTsContext
=====================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Solution.GenerateTypeScriptContext(Env.dev, Path.xrmDefinitelyTyped, Path.xrmTypings,
  solutions = [
    XrmSolution.name
    ],
  entities = [
    // eg. "systemuser"
    ],
  extraArguments = [
    "web", ""
    "jsLib", Path.jsLib
    ])