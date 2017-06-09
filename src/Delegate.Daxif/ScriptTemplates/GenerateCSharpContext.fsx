(**
SolutionUpdateCustomContext
=====================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Solution.GenerateCSharpContext(Env.dev, Path.xrmContext, Path.businessDomain,
  solutions = [
    XrmSolution.name
    ],
  entities = [
    // eg. "systemuser"
    ])
