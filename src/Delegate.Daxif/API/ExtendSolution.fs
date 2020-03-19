namespace DG.Daxif

open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Modules.Solution

type ExtendSolution private () =

  /// <summary>Extends a solution package from a given environment. Run this after solution export</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Extend(env: Environment, solutionName, outputDirectory, managed, ?logLevel) =
    log.setLevelOption logLevel
    Extend.extendSolution env solutionName outputDirectory managed

  /// <summary>Starts pre-import task for an extended solution. Run this before solution import</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member PreImport(env: Environment, solutionName, pathToSolutionZip, ?logLevel) =
    log.setLevelOption logLevel
    Extend.preImportExtendedSolution env solutionName pathToSolutionZip

  /// <summary>Starts post-import task for an extended solution. Run this after solution import</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member PostImport(env: Environment, solutionName, pathToSolutionZip, ?logLevel) =
    log.setLevelOption logLevel
    Extend.postImportExtendedSolution env solutionName pathToSolutionZip
