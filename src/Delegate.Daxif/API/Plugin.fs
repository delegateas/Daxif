namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Plugin
module sol = DG.Daxif.Modules.Solution.Main
open Utility

type Plugin private () =

  /// <summary>Updates plugin registrations in CRM based on the plugins found in your local assembly.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Sync(env: Environment, assemblyPath: string, projectPath: string, solutionName: string, ?dryRun: bool, ?isolationMode: PluginIsolationMode, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetProxy
    log.setLevelOption logLevel

    let dryRun = dryRun ?| false
    let isolationMode = isolationMode ?| PluginIsolationMode.Sandbox
    
    Main.syncSolution proxyGen assemblyPath projectPath solutionName isolationMode dryRun |> ignore


  /// <summary>Activates or deactivates all plugin steps of a solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member EnableSolutionPluginSteps(env: Environment, solutionName, ?enable, ?logLevel) =
    sol.enablePluginSteps env solutionName enable logLevel