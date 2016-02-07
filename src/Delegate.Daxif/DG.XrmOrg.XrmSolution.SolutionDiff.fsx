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
cfg.ensureFolder cfg.solutions

let zipSource = cfg.solutions + cfg.solution + @".zip"
let zipTarget = cfg.solutions + cfg.solution + @"_" + @".zip"

Diff.summary zipSource zipTarget cfg.log |> ignore

Diff.solution zipSource zipTarget cfg.log
