(**
SolutionPack
============

Libraries
---------

Config information *)
#load @"Config.fsx"

module cfg = Config

(** Open libraries for use *)
open DG.Daxif.Modules

(**
DAXIF# operations
-----------------

Pack solution *)
cfg.ensureFolder cfg.solutions

let map  = cfg.rootFolder + @"\..\Blueprint\xml"
let cms  = cfg.rootFolder + @"\..\Blueprint\customizations"

let zipu = cfg.solutions + cfg.solution + @"_.zip"
let zipm = cfg.solutions + cfg.solution + @"_managed_.zip"

Solution.pack
  cfg.solution zipu cms map false cfg.log

Solution.pack
  cfg.solution zipm cms map true cfg.log