module DG.Daxif.Modules.Data.Main

open System
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let exists (env: Environment) entityName filter log = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Data exists:")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  env.logAuthentication log'
  let guid = DataHelper.exists' env entityName filter log'
  log'.WriteLine
    (LogLevel.Info, 
      @"The data was retrieved successfully " + guid.ToString())
  guid  

let count proxyGen entityNames =
  let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
  log.Info "%s" daxifVersion
  log.Info "Data count: %s" entityNames'
  DataHelper.count' proxyGen entityNames
  log.Info "The data count was retrieved successfully"

let updateState (env: Environment) entityName filter state log = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Data state update:")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  env.logAuthentication log'
  DataHelper.updateState'' env entityName filter state log' |> ignore
  log'.WriteLine(LogLevel.Info, @"The data state was updated successfully")
  
let reassignAllRecords (env: Environment) userFrom userTo log = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Data assignment:")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  env.logAuthentication log'
  DataHelper.reassignAllRecords'' env userFrom userTo log' |> ignore
  log'.WriteLine(LogLevel.Info, @"The data was assigned successfully")
  
let export (env: Environment) location entityNames log serialize = 
  let log' = ConsoleLogger log
  let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Export data: " + entityNames')
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  env.logAuthentication log'
  DataHelper.export' env location entityNames log' serialize
  log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
  
let exportDelta (env: Environment) location entityNames date log serialize = 
  let log' = ConsoleLogger log
  let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Export data: " + entityNames')
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  env.logAuthentication log'
  DataHelper.exportDelta' env location entityNames date log' serialize
  log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
  
let exportView (env: Environment) location view user log serialize = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Export data based on view: " + view)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  env.logAuthentication log'
  DataHelper.exportView' env location view user log' serialize
  log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
  
let migrate (env: Environment) location log serialize map = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Migrate data")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  env.logAuthentication log'
  DataHelper.migrate' env location log' serialize map
  log'.WriteLine(LogLevel.Info, @"The data was migrated successfully")
  
let import (env: Environment) location log serialize additionalAttributes guidRemapping includeAttributes includeReferences referenceFilter = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Import data")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  env.logAuthentication log'
  DataHelper.import'' env location log' serialize includeAttributes includeReferences referenceFilter additionalAttributes guidRemapping
  log'.WriteLine(LogLevel.Info, @"The data was imported successfully")

 
let reassignOwner (env: Environment) location log serialize data = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Reassign data to owner")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  env.logAuthentication log'
  DataHelper.reassignOwner'' env location log' serialize data
  log'.WriteLine(LogLevel.Info, @"The data was reassigned successfully")
  
let associationImport (env: Environment) location log serialize guidRemapping = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Import relations")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  env.logAuthentication log'
  DataHelper.associationImport' env location log' serialize guidRemapping
  log'.WriteLine(LogLevel.Info, @"The relations were imported successfully")

let publishDuplicateDetectionRules (env: Environment) dupRules log= 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Publish Duplicate Detection Rules")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + env.url.ToString())
  env.logAuthentication log'
  DataHelper.DuplicateDetectionRules.publish env dupRules log'
  log'.WriteLine(LogLevel.Info, @"The Duplicate Detection Rules were published succesfully")
