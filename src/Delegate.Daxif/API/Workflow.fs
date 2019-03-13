namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Workflow
open DG.Daxif.Modules.Plugin.Domain
open Utility
open InternalUtility


type Workflow private () =

  /// <summary>Synchronizes all CodeActivities found in your local assembly to CRM.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Sync(env: Environment, assemblyPath: string, solutionName: string, ?isolationMode: PluginIsolationMode, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetProxy
    log.setLevelOption logLevel

    let isolationMode = isolationMode ?| PluginIsolationMode.Sandbox

    Main.syncSolution proxyGen solutionName assemblyPath isolationMode