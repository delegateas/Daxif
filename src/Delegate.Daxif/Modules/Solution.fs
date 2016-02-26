namespace DG.Daxif.Modules

open System
open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility

module Solution = 
  let createPublisher org name display prefix ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Create publisher: " + display)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Name: " + name)
    log'.WriteLine(LogLevel.Verbose, @"Display name: " + display)
    log'.WriteLine(LogLevel.Verbose, @"Prefix: " + prefix)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      SolutionHelper.createPublisher' org ac name display prefix log'
      log'.WriteLine(LogLevel.Info, @"The publisher was created successfully.")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let create org name display pubPrefix ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Create solution: " + display)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Name: " + name)
    log'.WriteLine(LogLevel.Verbose, @"Display name: " + display)
    log'.WriteLine(LogLevel.Verbose, @"Publisher prefix: " + pubPrefix)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      SolutionHelper.create' org ac name display pubPrefix log'
      log'.WriteLine(LogLevel.Info, @"The solution was created successfully.")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let delete org solution ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Delete solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      SolutionHelper.delete' org ac solution log'
      log'.WriteLine(LogLevel.Info, @"The deleted was created successfully.")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let pluginSteps org solution enable ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"PluginSteps solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      SolutionHelper.pluginSteps' org ac solution enable log'
      let msg' = 
        enable |> function 
        | true -> "enabled"
        | false -> "disabled"
      log'.WriteLine
        (LogLevel.Info, @"The solution plugins were successfully " + msg')
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let workflow org solution enable ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"WorkflowActivities solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      SolutionHelper.workflow' org ac solution enable log'
      let msg' = 
        enable |> function 
        | true -> "enabled"
        | false -> "disabled"
      log'.WriteLine
        (LogLevel.Info, 
         @"The solution workflow activities were successfully " + msg')
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let export org solution location managed ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Export solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine(LogLevel.Verbose, @"Managed solution: " + managed.ToString())
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      SolutionHelper.export' org ac solution location managed log'
      log'.WriteLine(LogLevel.Info, @"The solution was exported successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let import org solution location managed ap usr pwd domain log = 
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Import solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
    log'.WriteLine(LogLevel.Verbose, @"Managed solution: " + managed.ToString())
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      SolutionHelper.import' org ac solution location managed log' |> ignore
      log'.WriteLine(LogLevel.Info, @"The solution was imported successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  // TODO: 
  let extract solution location customizations map project log = 
    let log' = ConsoleLogger.ConsoleLogger log
    log'.WriteLine(LogLevel.Info, @"Extract solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
    try 
      SolutionHelper.extract' location customizations map project log' log
      log'.WriteLine(LogLevel.Info, @"The solution was extracted successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  // TODO: 
  let pack solution location customizations map managed log = 
    let log' = ConsoleLogger.ConsoleLogger log
    log'.WriteLine(LogLevel.Info, @"Pack solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
    try 
      SolutionHelper.pack' location customizations map managed log' log
      log'.WriteLine(LogLevel.Info, @"The solution was packed successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  // TODO: 
  let updateServiceContext org location ap usr pwd domain exe lcid log = 
    let org' : Uri = org
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Update service context:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      log'.WriteLine(LogLevel.Info, @"Updating the service context...")
      SolutionHelper.updateServiceContext' org' location ap usr pwd domain exe 
        lcid log'
      log'.WriteLine
        (LogLevel.Info, @"The service context was updated successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  let updateCustomServiceContext org location ap usr pwd domain exe log 
      solutions entities extraArgs = 
    let org' : Uri = org
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Update custom service context:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      log'.WriteLine(LogLevel.Info, @"Updating the custom service context...")
      SolutionHelper.updateCustomServiceContext' org' location ap usr pwd domain 
        exe log' solutions entities extraArgs
      log'.WriteLine
        (LogLevel.Info, @"The custom service context was updated successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
  
  // TODO: 
  let updateTypeScriptContext org location ap usr pwd domain exe log solutions 
      entities extraArgs = 
    let org' : Uri = org
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Update service context:")
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
    log'.WriteLine
      (LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      log'.WriteLine
        (LogLevel.Info, 
         @"Updating the TypeScript context in the WebResource folder...")
      SolutionHelper.updateTypeScriptContext' org' location ap usr pwd domain 
        exe log' solutions entities extraArgs
      log'.WriteLine
        (LogLevel.Info, @"The service context was updated successfully")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)

  // Counts all the components in the solution.
  let count org solution ap usr pwd domain log = 
    let org' : Uri = org
    let ac = Authentication.getCredentials ap usr pwd domain
    let log' = ConsoleLogger.ConsoleLogger log
    let pwd' = String.replicate pwd.Length "*"
    log'.WriteLine(LogLevel.Info, @"Count components in: " + solution)
    log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
    log'.WriteLine(LogLevel.Verbose, @"Authentication Provider: " + ap.ToString())
    log'.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log'.WriteLine(LogLevel.Verbose, @"Password: " + pwd')
    log'.WriteLine(LogLevel.Verbose, @"Domain: " + domain)
    try 
      let _count = SolutionHelper.count' org' solution ac log'
      log'.WriteLine
        (LogLevel.Info, @"The solution components were counted successfully")
      log'.WriteLine
        (LogLevel.Info, @"The solution contains " + string _count + " components")
    with ex -> log'.WriteLine(LogLevel.Error, getFullException ex)
