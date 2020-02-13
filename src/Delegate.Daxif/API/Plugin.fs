namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Modules.Plugin
open Domain
module SolutionMain = DG.Daxif.Modules.Solution.Main

type Plugin private () =

  /// <summary>Updates plugin registrations in CRM based on the plugins found in your local assembly.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Sync(env: Environment, assemblyPath: string, projectPath: string, solutionName: string, ?dryRun: bool, ?isolationMode: AssemblyIsolationMode, ?ignoreOutdatedAssembly: bool, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel

    let dryRun = dryRun ?| false
    let isolationMode = isolationMode ?| AssemblyIsolationMode.Sandbox
    let ignoreOutdatedAssembly = ignoreOutdatedAssembly ?| false
    
    Main.syncSolution proxyGen projectPath assemblyPath solutionName isolationMode ignoreOutdatedAssembly dryRun |> ignore


  /// <summary>Activates or deactivates all plugin steps of a solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member EnableSolutionPluginSteps(env: Environment, solutionName, ?enable, ?logLevel) =
    SolutionMain.enablePluginSteps env solutionName enable logLevel