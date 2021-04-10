namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Workflow
open Utility
open InternalUtility


type Workflow private () =

  /// <summary>Synchronizes all CodeActivities found in your local assembly to CRM.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="assemblyPath">Path to the workflow activity assembly dll to be synced (usually under the project bin folder).</param>
  /// <param name="solutionName">The name of the solution to which to sync workflow activities</param>
  /// <param name="isolationMode">Assembly Isolation Mode ('Sandbox' or 'None'). All Online environments must use 'Sandbox' - defaults to: 'Sandbox'</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Sync(env: Environment, assemblyPath: string, solutionName: string, ?isolationMode: AssemblyIsolationMode, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel

    let isolationMode = isolationMode ?| AssemblyIsolationMode.Sandbox

    Main.syncSolution proxyGen solutionName assemblyPath isolationMode