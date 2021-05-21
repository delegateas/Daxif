namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Translations
open Utility
open InternalUtility

type Translations private () =

  /// <summary>Exports translations from a given solution to a given directory.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="solutionName">The name of the solution from which to export translations.</param>
  /// <param name="outputDir">The directory into which the translation file will be saved (SolutionName + '_Translations.xip').</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Export(env: Environment, solutionName, outputDir, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel
    Main.export proxyGen solutionName outputDir

  /// <summary>Imports translations to a given solution from a given directory.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="solutionName">The name of the solution to which to import translations.</param>
  /// <param name="outputDir">The directory from which the translation file will be loaded.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Import(env: Environment, solutionName, outputDir, ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel
    Main.import proxyGen solutionName outputDir