namespace DG.Daxif

open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Modules.Solution

type ExtendedSolution private () =

  /// <summary>Extends an exported solution package from a given environment. Run this after solution export</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToSolutionZip">A relative or absolute path to solution file.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  /// <param name="timeOut">DataVerse Service Client timeout represented as a TimeSpan. - defaults to: 1 hour</param>
  static member Export(env: Environment, pathToSolutionZip, ?logLevel, ?timeOut: System.TimeSpan) =
    let timeOut = timeOut ?| defaultServiceTimeOut
    log.setLevelOption logLevel
    Main.extendSolution env pathToSolutionZip timeOut

  /// <summary>Starts pre-import task for an extended solution. Run this before solution import</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToSolutionZip">A relative or absolute path to solution file.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  /// <param name="timeOut">DataVerse Service Client timeout represented as a TimeSpan. - defaults to: 1 hour</param>
  static member PreImport(env: Environment, pathToSolutionZip, ?logLevel, ?timeOut: System.TimeSpan) =
    let timeOut = timeOut ?| defaultServiceTimeOut
    log.setLevelOption logLevel
    Main.preImportExtendedSolution env pathToSolutionZip timeOut

  /// <summary>Starts post-import task for an extended solution. Run this after solution import</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToSolutionZip">A relative or absolute path to solution file.</param>
  /// <param name="reassignWorkflows">Flag whether to reassign workflows based on environment from which the solution was exported. - defaults to: false</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  /// <param name="timeOut">DataVerse Service Client timeout represented as a TimeSpan. - defaults to: 1 hour</param>
  static member PostImport(env: Environment, pathToSolutionZip, ?reassignWorkflows, ?logLevel, ?timeOut: System.TimeSpan) =
    let timeOut = timeOut ?| defaultServiceTimeOut
    log.setLevelOption logLevel
    let reassignWorkflows = reassignWorkflows ?| false
    Main.postImportExtendedSolution env pathToSolutionZip reassignWorkflows timeOut
