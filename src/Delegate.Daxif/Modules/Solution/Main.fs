module DG.Daxif.Modules.Solution.Main

open System
open System.IO
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

let createPublisher org name display prefix ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log
  log.Info @"Create publisher: %s" display
  log.Verbose @"Organization: %O" org
  log.Verbose @"Name: %s" name
  log.Verbose @"Display name: %s" display
  log.Verbose @"Prefix: %s" prefix
  logAuthentication ap usr pwd' domain log
  SolutionHelper.createPublisher' org ac name display prefix log
  log.Info @"The publisher was created successfully."
  
let create org name display pubPrefix ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log

  log.Info @"Create solution: %s" display
  log.Verbose @"Organization: %O" org
  log.Verbose @"Name: %s" name
  log.Verbose @"Display name: %s" display
  log.Verbose @"Publisher prefix: %s" pubPrefix
  logAuthentication ap usr pwd' domain log
  SolutionHelper.create' org ac name display pubPrefix log
  log.Info @"The solution was created successfully."
  
let delete org solution ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log

  log.Info @"Delete solution: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  logAuthentication ap usr pwd' domain log
  SolutionHelper.delete' org ac solution log
  log.Info @"The deleted was created successfully."

let merge org sourceSolution targetSolution ap usr pwd domain log =
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log

  log.Info @"Merging %s solution into %s: " targetSolution sourceSolution
  log.Verbose @"Organization: %O" org
  logAuthentication ap usr pwd' domain log
  SolutionHelper.merge' org ac sourceSolution targetSolution log
  log.Info @"The solutions was merged successfully."
  
let pluginSteps org solution enable ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log

  log.Info @"PluginSteps solution: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  logAuthentication ap usr pwd' domain log
  SolutionHelper.pluginSteps' org ac solution enable log
  let msg' = 
    enable |> function 
    | true -> "enabled"
    | false -> "disabled"
  log.Info @"The solution plugins was successfully %s" msg'

let enablePluginSteps (env: Environment) solutionName enable logLevel =
  let usr, pwd, dmn = env.getCreds()
  let logLevel = logLevel ?| LogLevel.Verbose
  let enable = enable ?| true

  pluginSteps env.url solutionName enable env.apToUse usr pwd dmn logLevel

let workflow org solution enable ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log
  log.Info @"WorkflowActivities solution: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  logAuthentication ap usr pwd' domain log
  SolutionHelper.workflow' org ac solution enable log
  let msg' = 
    enable |> function 
    | true -> "enabled"
    | false -> "disabled"
  log.WriteLine
    (LogLevel.Info, 
      @"The solution workflow activities was successfully " + msg')
  
let export org solution location managed ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log
  log.Info @"Export solution: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  log.Verbose @"Managed solution: %O" managed
  logAuthentication ap usr pwd' domain log
  SolutionHelper.export' org ac solution location managed log
  log.Info @"The solution was exported successfully"
  
let import org location ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  let solution, managed = CrmUtility.getSolutionInformationFromFile location
  logVersion log
  log.Info @"Import solution: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" location
  log.Verbose @"Managed solution: %O" managed
  logAuthentication ap usr pwd' domain log
  SolutionHelper.import' org ac solution location managed log |> ignore
  log.Info @"The solution was imported successfully"

let exportWithExtendedSolution org solution location managed ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let ac' = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log
  log.Info @"Export solution: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to folder: %s" location
  log.Verbose @"Managed solution: %O" managed
  logAuthentication ap usr pwd' domain log
  SolutionHelper.exportWithExtendedSolution' org ac ac' solution location managed log
  log.Info @"The extended solution was exported successfully"
  
let importWithExtendedSolution org location ap usr pwd domain log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let ac' = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  let solution, managed = CrmUtility.getSolutionInformationFromFile location
  logVersion log
  log.Info @"Import solution: %s" solution
  log.Verbose @"Organization: %O" org
  log.Verbose @"Solution: %s" solution
  log.Verbose @"Path to file: %s" location
  log.Verbose @"Managed solution: %O" managed
  logAuthentication ap usr pwd' domain log
  SolutionHelper.importWithExtendedSolution' org ac ac' solution location managed log |> ignore
  log.Info @"The extended solution was imported successfully"
  
// TODO: 
let extract location customizations map project logLevel = 
  let log = ConsoleLogger logLevel
  logVersion log
  log.Verbose @"Path to file: %s" location
  SolutionHelper.extract' location customizations map project log logLevel
  log.Info @"The solution was extracted successfully"
  
// TODO: 
let pack location customizations map managed logLevel = 
  let log = ConsoleLogger logLevel
  logVersion log
  log.Verbose @"Path to file: %s" location
  SolutionHelper.pack' location customizations map managed log logLevel
  log.Info @"The solution was packed successfully"
  
// TODO: 
let updateServiceContext org location ap usr pwd domain exe lcid log = 
  let org' : Uri = org
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log
  log.Info @"Update service context:"
  log.Verbose @"Organization: %O" org
  log.Verbose @"Path to folder: %s" location
  logAuthentication ap usr pwd' domain log
  SolutionHelper.updateServiceContext' org' location ap usr pwd domain exe 
    lcid log
  log.Info @"The service context was updated successfully"
  
let updateCustomServiceContext org outputDirectory ap usr (pwd: string) domain pathToExe log 
    solutions entities extraArgs = 
  let org' : Uri = org
  let logger = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %O" org
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  logAuthentication ap usr pwd' domain logger
  logger.Info "Updating the C# context..."

  SolutionHelper.updateCustomServiceContext' org' outputDirectory ap usr pwd domain 
    pathToExe logger solutions entities extraArgs
  logger.Info "The C# context was updated successfully"


let updateXrmMockupMetadata org outputDirectory ap usr (pwd: string) domain pathToExe log 
    solutions entities extraArgs = 
  let org' : Uri = org
  let logger = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %O" org
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  logAuthentication ap usr pwd' domain logger
  logger.Info "Updating XrmMockup Metadata..."

  SolutionHelper.updateXrmMockupMetadata' org' outputDirectory ap usr pwd domain 
    pathToExe logger solutions entities extraArgs
  logger.Info "XrmMockup Metadata was updated successfully"
  

let updateTypeScriptContext org outputDirectory ap usr (pwd: string) domain pathToExe log solutions 
    entities extraArgs = 
  let org' : Uri = org
  let logger = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logger.Info "%s" daxifVersion
  logger.Info "Organization: %O" org
  logger.Info "Path to output dir: %s" (Path.GetFullPath outputDirectory)
  logAuthentication ap usr pwd' domain logger
  logger.Info "Updating the TypeScript context..."

  SolutionHelper.updateTypeScriptContext' org' outputDirectory ap usr pwd domain 
    pathToExe logger solutions entities extraArgs
  logger.Info "The TypeScript context was updated successfully"

// Counts all the components in the solution.
let count org solution ap usr pwd domain log = 
  let org' : Uri = org
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let log = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  logVersion log
  log.Info @"Count components in: %s" solution
  log.Verbose @"Organization: %O" org
  logAuthentication ap usr pwd' domain log
  let _count = SolutionHelper.count' org' solution ac
  log.Info @"The solution components were counted successfully"
  log.Info @"The solution contains %d components" _count