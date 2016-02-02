(**
SolutionDiff
===============

Libraries
---------

Config information *)
#load @"DG.XrmOrg.XrmSolution.Config.fsx"

module cfg = DG.XrmOrg.XrmSolution.Config

(** Open libraries for use *)
open DG.Daxif.Modules

(**
DAXIF# operations
-----------------

Extract solution *)
cfg.ensureFolder cfg.unmanaged

let map =   cfg.rootFolder + @"\..\..\Solution\DG.XrmOrg.XrmSolution.xml"
let cms =   cfg.rootFolder + @"\..\..\Solution\customizations"
let vsSol = cfg.rootFolder + @"\..\..\Solution\Solution.csproj"

let zipSource = cfg.unmanaged + cfg.solution + @".zip"
let zipTarget = cfg.unmanaged + cfg.solution + @"_" + @".zip"

Diff.solution zipSource zipTarget cfg.log

Diff.summary zipSource zipTarget cfg.log |> ignore
