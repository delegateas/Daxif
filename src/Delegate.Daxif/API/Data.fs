namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Data
open Utility
open InternalUtility

type Data private () =

  /// <summary>Counts the amount of records for the given entity logical names.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Count(env: Environment, entityNames, ?logLevel) =
    let proxyGen = env.connect(log).GetProxy
    log.setLevelOption logLevel
    Main.count proxyGen entityNames


  /// <summary>Imports data from a given file.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Import(env: Environment, pathToData, ?serialize, ?guidRemapping, ?additionalAttributes, ?includeAttributes, ?includeReferences, ?referenceFilter, ?logLevel) =
    let usr, pwd, dmn = env.getCreds()
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let serialize = serialize ?| Serialize.JSON
    let includeAttributes = includeAttributes ?| true
    let includeReferences = includeReferences ?| true
    let referenceFilter = referenceFilter ?| [||]
    let additionalAttributes = additionalAttributes ?| Map.empty
    let guidRemapping = guidRemapping ?| Map.empty

    Main.import env.url pathToData env.apToUse usr pwd dmn logLevel serialize additionalAttributes guidRemapping includeAttributes includeReferences referenceFilter


  /// <summary>Exports data from given entities to a file.</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Export(env: Environment, entityNames, pathToOutputFile, ?deltaFromDate, ?logLevel) =
    let usr, pwd, dmn = env.getCreds()
    let logLevel = logLevel ?| LogLevel.Verbose

    match deltaFromDate with
    | Some date -> Main.exportDelta env.url pathToOutputFile entityNames date env.apToUse usr pwd dmn logLevel
    | None      -> Main.export env.url pathToOutputFile entityNames env.apToUse usr pwd dmn logLevel
