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
    log'.WriteLine(LogLevel.Info, @"Data exists:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      let guid = DataHelper.exists' org ac entityName filter log'
      log'.WriteLine
        (LogLevel.Info, 
         @"The data was retrieved successfully " + guid.ToString())
      guid
    with ex -> 
      log'.WriteLine(LogLevel.Error, getFullException ex)
      Guid.Empty
  
  let count org entityNames ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Data count: " + entityNames')
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.count' org ac entityNames log'
      log'.WriteLine
        (LogLevel.Info, @"The data count was retrieved successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let updateState org entityName filter state ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Data state update:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.updateState'' org ac entityName filter state log' |> ignore
      log'.WriteLine(LogLevel.Info, @"The data state was updated successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let reassignAllRecords org userFrom userTo ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Data assignment:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.reassignAllRecords'' org ac userFrom userTo log' |> ignore
      log'.WriteLine(LogLevel.Info, @"The data was assigned successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let export org location entityNames ap usr pwd domain log serialize = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Export data: " + entityNames')
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.export' org ac location entityNames log' serialize
      log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let exportDelta org location entityNames date ap usr pwd domain log serialize = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let entityNames' = entityNames |> Array.reduce (fun x y -> x + "," + y)
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Export data: " + entityNames')
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.exportDelta' org ac location entityNames date log' serialize
      log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let exportView org location view user ap usr pwd domain log serialize = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Export data based on view: " + view)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.exportView' org ac location view user log' serialize
      log'.WriteLine(LogLevel.Info, @"The data was exported successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let migrate org location ap usr pwd domain log serialize map = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Migrate data")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.migrate' org ac location log' serialize map
      log'.WriteLine(LogLevel.Info, @"The data was migrated successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let import org location ap usr pwd domain log serialize attribs data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Import data")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.import'' org ac location log' serialize attribs data
      log'.WriteLine(LogLevel.Info, @"The data was imported successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let reassignOwner org location ap usr pwd domain log serialize data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Reassign data to owner")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.reassignOwner'' org ac location log' serialize data
      log'.WriteLine(LogLevel.Info, @"The data was reassigned successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let associationImport org location ap usr pwd domain log serialize data = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Import relations")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      DataHelper.associationImport' org ac location log' serialize data
      log'.WriteLine(LogLevel.Info, @"The relations were imported successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
