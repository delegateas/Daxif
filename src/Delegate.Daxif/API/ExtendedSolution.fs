namespace DG.Daxif

open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Modules.Solution

type ExtendedSolution private () =

  /// <summary>Extends an exported solution package from a given environment. Run this after solution export</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Export(env: Environment, pathToSolutionZip, ?logLevel) =
    log.setLevelOption logLevel
    Main.extendSolution env pathToSolutionZip

  /// <summary>Starts pre-import task for an extended solution. Run this before solution import</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member PreImport(env: Environment, pathToSolutionZip, ?logLevel) =
    log.setLevelOption logLevel
    Main.preImportExtendedSolution env pathToSolutionZip

  /// <summary>Starts post-import task for an extended solution. Run this after solution import</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member PostImport(env: Environment, pathToSolutionZip, ?reassignWorkflows, ?logLevel) =
    log.setLevelOption logLevel
    let reassignWorkflows = reassignWorkflows ?| false
    Main.postImportExtendedSolution env pathToSolutionZip reassignWorkflows
