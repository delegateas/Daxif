namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Translations
open Utility

type Translations private () =

  /// <summary>Exports translations from a given solution to a given directory.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Export(env: Environment, solutionName, outputDir, ?logLevel: LogLevel) =
    let usr, pwd, dmn = env.getCreds()
    let logLevel = logLevel ?| LogLevel.Verbose

    Main.export env.url solutionName outputDir env.apToUse usr pwd dmn logLevel

  /// <summary>Imports translations to a given solution from a given directory.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Import(env: Environment, solutionName, outputDir, ?logLevel: LogLevel) =
    let usr, pwd, dmn = env.getCreds()
    let logLevel = logLevel ?| LogLevel.Verbose

    Main.import env.url solutionName outputDir env.apToUse usr pwd dmn logLevel