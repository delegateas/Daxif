namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Info
open Utility

type Info private () =

  /// <summary>Retrieves the CRM version of the targeted environment.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member RetrieveVersion(env: Environment, ?logLevel: LogLevel) =
    let usr, pwd, dmn = env.getCreds()
    let logLevel = logLevel ?| LogLevel.Verbose

    Main.version env.url env.apToUse usr pwd dmn logLevel
