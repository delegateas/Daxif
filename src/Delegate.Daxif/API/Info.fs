namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Info
open Utility
open InternalUtility

type Info private () =

  /// <summary>Retrieves the CRM version of the targeted environment.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member RetrieveVersion(env: Environment, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel
    Main.version proxyGen
