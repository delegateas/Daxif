(**
DataExportSource
=================
*)
#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif

let entities = 
  [|
    "account"
    "contact"
  |]

Data.Export(cfg.devEnv, entities, cfg.Path.data)