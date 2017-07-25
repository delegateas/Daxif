namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Workflow

open Utility

type Workflow private () =

  /// <summary>Synchronizes all CodeActivities found in your local assembly to CRM.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Sync(env: Environment, assemblyPath: string, solutionName: string, ?logLevel: LogLevel) =
    let usr, pwd, dmn = env.getCreds()
    let logLevel = logLevel ?| LogLevel.Verbose
    
    Main.syncSolution env.url solutionName assemblyPath env.apToUse usr pwd dmn logLevel