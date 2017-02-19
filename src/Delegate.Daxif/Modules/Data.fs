namespace DG.Daxif.Modules

open System
open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility

module Data = 
  let exists org entityName filter ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Data exists:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    logAuthentication ap usr pwd' domain log'
    let guid = DataHelper.exists' org ac entityName filter log'
    log'.WriteLine
      (LogLevel.Info, 
        @"The data was retrieved successfully " + guid.ToString())
    guid

  let count org entityNames ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Data count: " + entityNames')
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    logAuthentication ap usr pwd' domain log'
    DataHelper.count' org ac entityNames log'
    log'.WriteLine
      (LogLevel.Info, @"The data count was retrieved successfully")

  let updateState org entityName filter state ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Data state update:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    logAuthentication ap usr pwd' domain log'
    DataHelper.updateState'' org ac entityName filter state log' |> ignore
    log'.WriteLine(LogLevel.Info, @"The data state was updated successfully")
  
  let reassignAllRecords org userFrom userTo ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Data assignment:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    logAuthentication ap usr pwd' domain log'
    DataHelper.reassignAllRecords'' org ac userFrom userTo log' |> ignore
    log'.WriteLine(LogLevel.Info, @"The data was assigned successfully")
  
  let export org location entityNames ap usr pwd domain log serialize = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Export data: " + entityNames')
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.export' org ac location entityNames log' serialize
    log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
  
  let exportDelta org location entityNames date ap usr pwd domain log serialize = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Export data: " + entityNames')
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.exportDelta' org ac location entityNames date log' serialize
    log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
  
  let exportView org location view user ap usr pwd domain log serialize = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Export data based on view: " + view)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.exportView' org ac location view user log' serialize
    log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
  
  let migrate org location ap usr pwd domain log serialize map = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Migrate data")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.migrate' org ac location log' serialize map
    log'.WriteLine(LogLevel.Info, @"The data was migrated successfully")
  
  let import org location ap usr pwd domain log serialize attribs data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Import data")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.import'' org ac location log' serialize true true [||] attribs data
    log'.WriteLine(LogLevel.Info, @"The data was imported successfully")

  let importWithoutReferences org location ap usr pwd domain log serialize attribs data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Import data")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.import'' org ac location log' serialize true false [||] attribs data
    log'.WriteLine(LogLevel.Info, @"The data was imported successfully")

  let importReferences org location ap usr pwd domain log serialize referenceFilter attribs data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Import data")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.import'' org ac location log' serialize false true referenceFilter attribs data
    log'.WriteLine(LogLevel.Info, @"The data was imported successfully")
 
  let reassignOwner org location ap usr pwd domain log serialize data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Reassign data to owner")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.reassignOwner'' org ac location log' serialize data
    log'.WriteLine(LogLevel.Info, @"The data was reassigned successfully")
  
  let associationImport org location ap usr pwd domain log serialize data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Import relations")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    logAuthentication ap usr pwd' domain log'
    DataHelper.associationImport' org ac location log' serialize data
    log'.WriteLine(LogLevel.Info, @"The relations were imported successfully")

  let publishDuplicateDetectionRules org dupRules ap usr pwd domain log= 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Publish Duplicate Detection Rules")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    logAuthentication ap usr pwd' domain log'
    DataHelper.DuplicateDetectionRules.publish org ac dupRules log'
    log'.WriteLine(LogLevel.Info, @"The Duplicate Detection Rules were published succesfully")
