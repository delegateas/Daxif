namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Data
open Utility
open InternalUtility

type Data private () =

  /// <summary>Counts the amount of records for the given entity logical names.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="entityNames">List of entity logical names to include in record count.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Count(env: Environment, entityNames, ?logLevel) =
    let proxyGen = env.connect(log).GetService
    log.setLevelOption logLevel
    Main.count proxyGen entityNames


  /// <summary>Imports data from a given file.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToData">A relative or absolute path to a data import file.</param>
  /// <param name="serialize">Serialization format ('BIN', 'XML' or 'JSON') - defaults to: 'JSON'</param>
  /// <param name="guidRemapping">Guid remapping map</param>
  /// <param name="additionalAttributes">Additional attributes to include in data import</param>
  /// <param name="includeAttributes">Flag whether or not to include attributes in data import - defaults to: true</param>
  /// <param name="includeReferences">Flag whether or not to include references (lookup attributes) in data import - defaults to: true</param>
  /// <param name="referenceFilter">List of filtering which lookup attributes to include in data import (based on logical name)</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Import(env: Environment, pathToData, ?serialize, ?guidRemapping, ?additionalAttributes, ?includeAttributes, ?includeReferences, ?referenceFilter, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let serialize = serialize ?| Serialize.JSON
    let includeAttributes = includeAttributes ?| true
    let includeReferences = includeReferences ?| true
    let referenceFilter = referenceFilter ?| [||]
    let additionalAttributes = additionalAttributes ?| Map.empty
    let guidRemapping = guidRemapping ?| Map.empty

    Main.import env pathToData logLevel serialize additionalAttributes guidRemapping includeAttributes includeReferences referenceFilter


  /// <summary>Exports data from given entities to a file.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="entityNames">List of entity logical names to include in data export.</param>
  /// <param name="pathToOutputFile">A relative or absolute path to a data export output file.</param>
  /// <param name="deltaFromDate">Export only entities with modified on or later than date given as DateTime.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Export(env: Environment, entityNames, pathToOutputFile, ?deltaFromDate, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose

    match deltaFromDate with
    | Some date -> Main.exportDelta env pathToOutputFile entityNames date logLevel
    | None      -> Main.export env pathToOutputFile entityNames logLevel
