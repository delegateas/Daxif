namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Workflow

open Utility
open InternalUtility

type Workflow private () =

  /// <summary>Synchronizes all CodeActivities found in your local assembly to CRM.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Sync(env: Environment, assemblyPath: string, solutionName: string, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetProxy
    log.setLevelOption logLevel
    Main.syncSolution proxyGen solutionName assemblyPath