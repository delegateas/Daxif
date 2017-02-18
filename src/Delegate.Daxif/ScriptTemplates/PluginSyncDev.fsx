(**
PluginSyncDev
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif

Plugin.Sync(cfg.devEnv, cfg.Path.pluginDll, cfg.Path.pluginProjFile, cfg.solutionName)
