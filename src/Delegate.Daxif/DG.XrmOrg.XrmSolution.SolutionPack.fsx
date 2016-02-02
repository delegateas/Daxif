(**
SolutionPack
============

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

Pack solution *)
cfg.ensureFolder cfg.unmanaged

let map =   cfg.rootFolder + @"\..\..\Solution\DG.XrmOrg.XrmSolution.xml"
let cms =   cfg.rootFolder + @"\..\..\Solution\customizations"

let zip = cfg.unmanaged + cfg.solution + @"_.zip"

Solution.pack
  cfg.solution zip cms map cfg.log
