(**
WorkflowSyncDev
=================
*)

#load @"_Config.fsx"
module cfg = _Config

open DG.Daxif

Workflow.Sync(cfg.devEnv, cfg.Path.workflowDll, cfg.solutionName)
