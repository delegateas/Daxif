(**
PluginSyncDev
=================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Plugin.Sync(devEnv, Path.pluginDll, Path.pluginProjFile, solutionName)
