namespace DG.Daxif

open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Modules.Plugin
module SolutionMain = DG.Daxif.Modules.Solution.Main

type Plugin private () =

  /// <summary>Updates plugin registrations in CRM based on the plugins found in your local assembly.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="assemblyPath">Path to the plugin assembly dll to be synced (usually under the project bin folder).</param>
  /// <param name="projectPath">Path to the plugin assembly project (.csproj).</param>
  /// <param name="solutionName">The name of the solution to which to sync plugins</param>
  /// <param name="dryRun">Flag whether or not to simulate/test syncing plugins (running a 'dry run'). - defaults to: false</param>
  /// <param name="isolationMode">Assembly Isolation Mode ('Sandbox' or 'None'). All Online environments must use 'Sandbox' - defaults to: 'Sandbox'</param>
  /// <param name="ignoreOutdatedAssembly">Flag whether or not to simulate/test syncing plugins (running a 'dry run'). - defaults to: false</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Sync(env: Environment, assemblyPath: string, projectPath: string, solutionName: string, ?dryRun: bool, ?isolationMode: AssemblyIsolationMode, ?ignoreOutdatedAssembly: bool, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel

    let dryRun = dryRun ?| false
    let isolationMode = isolationMode ?| AssemblyIsolationMode.Sandbox
    let ignoreOutdatedAssembly = ignoreOutdatedAssembly ?| false
    
    Main.syncSolution proxyGen projectPath assemblyPath solutionName isolationMode ignoreOutdatedAssembly dryRun |> ignore

  /// <summary>Activates or deactivates all plugin steps of a solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="solutionName">The name of the solution in which to enable or disable all plugins</param>
  /// <param name="enable">Flag whether to enable or disable all solution plugins. - defaults to: true</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member EnableSolutionPluginSteps(env: Environment, solutionName, ?enable, ?logLevel) =
    SolutionMain.enablePluginSteps env solutionName enable logLevel