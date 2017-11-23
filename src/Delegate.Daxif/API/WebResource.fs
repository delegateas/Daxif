namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.WebResource
open InternalUtility

open Utility

type WebResource private () =

  /// <summary>Updates the web resources in CRM based on the ones from your local web resource root.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Sync(env: Environment, webresourceRoot: string, solutionName: string, ?logLevel: LogLevel, ?webResourcePrefix: string) =
    let proxyGen = env.connect(log).GetProxy
    log.setLevelOption logLevel
    Main.syncSolution proxyGen solutionName webresourceRoot webResourcePrefix