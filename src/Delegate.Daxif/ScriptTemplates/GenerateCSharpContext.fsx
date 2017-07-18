(**
SolutionUpdateCustomContext
=====================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let xrmContext = Path.toolsFolder ++ @"XrmContext\XrmContext.exe"
let businessDomain = Path.solutionRoot ++ @"BusinessDomain"

Solution.GenerateCSharpContext(Env.dev, xrmContext, businessDomain,
  solutions = [
    SolutionInfo.name
    ],
  entities = [
    // eg. "systemuser"
    ],
  extraArguments = [
    "deprecatedprefix", "ZZ_"
    ])
