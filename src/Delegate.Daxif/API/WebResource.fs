namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.WebResource

open Utility

type WebResource private () =

  /// <summary>Updates the web resources in CRM based on the ones from your local web resource root.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Sync(env: Environment, webresourceRoot: string, solutionName: string, ?logLevel: LogLevel) =
    let usr, pwd, dmn = env.getCreds()
    let logLevel = logLevel ?| LogLevel.Verbose
    
    Main.syncSolution env.url solutionName webresourceRoot env.apToUse usr pwd dmn logLevel