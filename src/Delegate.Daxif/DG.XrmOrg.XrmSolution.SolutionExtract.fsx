(**
SolutionExtract
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

let map   = cfg.rootFolder + @"\..\..\Blueprint\DG.XrmOrg.XrmBlueprint.xml"
let cms   = cfg.rootFolder + @"\..\..\Blueprint\customizations"
let vsSol = cfg.rootFolder + @"\..\..\Blueprint\Blueprint.csproj"

let zip = cfg.solutions + cfg.solution + @".zip"

Solution.extract
  cfg.solution
    zip cms map vsSol
      cfg.log
