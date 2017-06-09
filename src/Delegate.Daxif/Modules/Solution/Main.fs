module DG.Daxif.Modules.Solution.Main

open System
open System.IO
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let createPublisher org name display prefix ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Create publisher: " + display)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Name: " + name)
  log'.WriteLine(LogLevel.Verbose, @"Display name: " + display)
  log'.WriteLine(LogLevel.Verbose, @"Prefix: " + prefix)
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.createPublisher' org ac name display prefix log'
  log'.WriteLine(LogLevel.Info, @"The publisher was created successfully.")
  
let create org name display pubPrefix ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Create solution: " + display)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Name: " + name)
  log'.WriteLine(LogLevel.Verbose, @"Display name: " + display)
  log'.WriteLine(LogLevel.Verbose, @"Publisher prefix: " + pubPrefix)
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.create' org ac name display pubPrefix log'
  log'.WriteLine(LogLevel.Info, @"The solution was created successfully.")
  
let delete org solution ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Delete solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.delete' org ac solution log'
  log'.WriteLine(LogLevel.Info, @"The deleted was created successfully.")

let merge org sourceSolution targetSolution ap usr pwd domain log =
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, 
    (sprintf @"Merging %s solution into %s: " targetSolution sourceSolution))
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.merge' org ac sourceSolution targetSolution log'
  log'.WriteLine(LogLevel.Info, @"The solutions was merged successfully.")
  
let pluginSteps org solution enable ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"PluginSteps solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.pluginSteps' org ac solution enable log'
  let msg' = 
    enable |> function 
    | true -> "enabled"
    | false -> "disabled"
  log'.WriteLine
    (LogLevel.Info, @"The solution plugins was successfully " + msg')

let enablePluginSteps (env: Environment) solutionName enable logLevel =
  let usr, pwd, dmn = env.getCreds()
  let logLevel = logLevel ?| LogLevel.Verbose
  let enable = enable ?| true

  pluginSteps env.url solutionName enable env.apToUse usr pwd dmn logLevel

let workflow org solution enable ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"WorkflowActivities solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.workflow' org ac solution enable log'
  let msg' = 
    enable |> function 
    | true -> "enabled"
    | false -> "disabled"
  log'.WriteLine
    (LogLevel.Info, 
      @"The solution workflow activities was successfully " + msg')
  
let export org solution location managed ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Export solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  log'.WriteLine(LogLevel.Verbose, @"Managed solution: " + managed.ToString())
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.export' org ac solution location managed log'
  log'.WriteLine(LogLevel.Info, @"The solution was exported successfully")
  
let import org location ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  let solution, managed = CrmUtility.getSolutionInformationFromFile location
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Import solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
  log'.WriteLine(LogLevel.Verbose, @"Managed solution: " + managed.ToString())
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.import' org ac solution location managed log' |> ignore
  log'.WriteLine(LogLevel.Info, @"The solution was imported successfully")

let exportWithDGSolution org solution location managed ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let ac' = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Export solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  log'.WriteLine(LogLevel.Verbose, @"Managed solution: " + managed.ToString())
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.exportWithDGSolution' org ac ac' solution location managed log'
  log'.WriteLine(LogLevel.Info, @"The extended solution was exported successfully")
  
let importWithDGSolution org location ap usr pwd domain log = 
  let ac = Authentication.getCredentials ap usr pwd domain
  let ac' = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  let solution, managed = CrmUtility.getSolutionInformationFromFile location
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Import solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Solution: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
  log'.WriteLine(LogLevel.Verbose, @"Managed solution: " + managed.ToString())
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.importWithDGSolution' org ac ac' solution location managed log' |> ignore
  log'.WriteLine(LogLevel.Info, @"The extended solution was imported successfully")
  
// TODO: 
let extract location customizations map project log = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
  SolutionHelper.extract' location customizations map project log' log
  log'.WriteLine(LogLevel.Info, @"The solution was extracted successfully")
  
// TODO: 
let pack location customizations map managed log = 
  let log' = ConsoleLogger log
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Verbose, @"Path to file: " + location)
  SolutionHelper.pack' location customizations map managed log' log
  log'.WriteLine(LogLevel.Info, @"The solution was packed successfully")
  
// TODO: 
let updateServiceContext org location ap usr pwd domain exe lcid log = 
  let org' : Uri = org
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Update service context:")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  log'.WriteLine(LogLevel.Verbose, @"Path to folder: " + location)
  logAuthentication ap usr pwd' domain log'
  SolutionHelper.updateServiceContext' org' location ap usr pwd domain exe 
    lcid log'
  log'.WriteLine
    (LogLevel.Info, @"The service context was updated successfully")
  
let updateCustomServiceContext org outputDirectory ap usr (pwd: string) domain pathToExe log 
    solutions entities extraArgs = 
  let org' : Uri = org
  let logger = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %s" (org.ToString())
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  logAuthentication ap usr pwd' domain logger
  logger.Info "Updating the C# context..."

  SolutionHelper.updateCustomServiceContext' org' outputDirectory ap usr pwd domain 
    pathToExe logger solutions entities extraArgs
  logger.Info "The C# context was updated successfully"
  

let updateTypeScriptContext org outputDirectory ap usr (pwd: string) domain pathToExe log solutions 
    entities extraArgs = 
  let org' : Uri = org
  let logger = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %s" (org.ToString())
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  logAuthentication ap usr pwd' domain logger
  logger.Info "Updating the TypeScript context..."

  SolutionHelper.updateTypeScriptContext' org' outputDirectory ap usr pwd domain 
    pathToExe logger solutions entities extraArgs
  logger.Info "The TypeScript context was updated successfully"

// Counts all the components in the solution.
let count org solution ap usr pwd domain log = 
  let org' : Uri = org
  let ac = Authentication.getCredentials ap usr pwd domain
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Count components in: " + solution)
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  logAuthentication ap usr pwd' domain log'
  let _count = SolutionHelper.count' org' solution ac
  log'.WriteLine
    (LogLevel.Info, @"The solution components were counted successfully")
  log'.WriteLine
    (LogLevel.Info, @"The solution contains " + string _count + " components")