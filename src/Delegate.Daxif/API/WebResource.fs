namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.WebResource
open InternalUtility

open Utility

type WebResource private () =

  /// <summary>Updates the web resources in CRM based on the ones from your local web resource root.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="webresourceRoot">Root folder of the Web Resources project.</param>
  /// <param name="solutionName">The name of the solution to which to sync web resources</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  /// <param name="patchSolutionName">The name of the patch solution to which to sync web resources.</param>
  static member Sync(env: Environment, webresourceRoot: string, solutionName: string, ?logLevel: LogLevel, ?patchSolutionName: string, ?publishAfterSync: bool) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel
    Main.syncSolution proxyGen solutionName webresourceRoot patchSolutionName publishAfterSync