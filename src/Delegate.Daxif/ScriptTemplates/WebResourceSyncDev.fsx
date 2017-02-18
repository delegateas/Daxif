(**
WebResouresSyncDev
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif

WebResource.Sync(cfg.devEnv, cfg.Path.webResourceSrc, cfg.solutionName)
