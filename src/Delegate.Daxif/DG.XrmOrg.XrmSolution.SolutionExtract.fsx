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
cfg.ensureFolder cfg.unmanaged

let map =   cfg.rootFolder + @"\..\..\Solution\DG.XrmOrg.XrmSolution.xml"
let cms =   cfg.rootFolder + @"\..\..\Solution\customizations"
let vsSol = cfg.rootFolder + @"\..\..\Solution\Solution.csproj"

let zip = cfg.unmanaged + cfg.solution + @".zip"

Solution.extract
  cfg.solution
    zip cms map vsSol
      cfg.log
