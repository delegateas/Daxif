(**
WorkflowSyncDev
=================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Workflow.Sync(Env.dev, Path.workflowDll, XrmSolution.name)
