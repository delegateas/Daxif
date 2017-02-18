(**
DataImportTarget
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif
open DG.Daxif.Modules

Data.Import(cfg.testEnv, cfg.Path.data, serialize = Serialize.JSON)
