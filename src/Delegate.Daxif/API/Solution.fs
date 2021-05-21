namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Modules.Solution
open Utility
open InternalUtility

type DiffImportCallingInfo = {
  SolutionName : string
}

type DiffExportCallingInfo = {
  TargetEnv : Environment
}

type Solution private () =

  /// <summary>Publish all customization. Not necessary after a import of an managed solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug</param>
  /// <param name="timeOut">DataVerse Service Client timeout represented as a TimeSpan. - defaults to: 15 minutes</param>
  /// <remarks>Due to Microsoft's PublishAll API Message often timing out first time after a Solution Import (although publish went through server-side), this function will retry once after timeout failure.</remarks>
  static member PublishAll(env: Environment, ?logLevel, ?timeOut: System.TimeSpan) =
    let timeOut = timeOut ?| System.TimeSpan(0,15,0)

    log.setLevelOption logLevel
    try
        Main.PublishAll env timeOut
    with
        | :? System.TimeoutException as ex -> 
            log.Warn "PublishAll timed out with message: %s" ex.Message
            log.Warn "Trying again..."
            Main.PublishAll env timeOut

  /// <summary>Imports a solution package from a given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToSolutionZip">A relative or absolute path to solution file.</param>
  /// <param name="activatePluginSteps">Flag whether or not to enable all solution plugins. - defaults to: false</param>
  /// <param name="extended">Flag whether or not to enable Delegate Extended Solution functionality. - defaults to: false</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  /// <param name="diffCallingInfo">[Experimental] When specified, a diff import will be performed. Assumes exportdiff has been used prior.</param>
  /// <param name="publishAfterImport">Flag whether or not to publish all customizations after solution import. - defaults to: true</param>
  /// <param name="reassignWorkflows">Flag whether to reassign workflows based on environment from which the solution was exported. - defaults to: false</param>
  /// <param name="timeOut">DataVerse Service Client timeout represented as a TimeSpan. - defaults to: 1 hour</param>
  static member Import(env: Environment, pathToSolutionZip, ?activatePluginSteps, ?extended, ?logLevel, ?diffCallingInfo, ?publishAfterImport, ?reassignWorkflows, ?timeOut: System.TimeSpan) =    
    let publishAfterImport = publishAfterImport ?| true
    let reassignWorkflows = reassignWorkflows ?| false
    let timeOut = timeOut ?| defaultServiceTimeOut

    match diffCallingInfo with
    | Some dci -> Main.importDiff pathToSolutionZip dci.SolutionName Domain.partialSolutionName env timeOut
    | _ -> Main.importStandard env activatePluginSteps extended publishAfterImport reassignWorkflows pathToSolutionZip logLevel timeOut

  /// <summary>Exports a solution package from a given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="solutionName">The name of the solution to export.</param>
  /// <param name="outputDirectory">The directory to which the solution file will be saved.</param>
  /// <param name="managed">Flag whether to export as Managed or Unmanaged solution.</param>
  /// <param name="extended">Flag whether or not to enable Delegate Extended Solution functionality. - defaults to: false</param>
  /// <param name="deltaFromDate">N/A - Not used</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  /// <param name="diffCallingInfo">[Experimental] When specified, a diff export happens with env as source and diffCallingInfo.TargetEnv as target</param>
  /// <param name="async">Flag whether to execute solution export asynchronously. - defaults to: false</param>
  /// <param name="timeOut">DataVerse Service Client timeout represented as a TimeSpan. - defaults to: 1 hour</param>
  static member Export(env: Environment, solutionName, outputDirectory, managed, ?extended, ?deltaFromDate, ?logLevel, ?diffCallingInfo, ?async, ?timeOut: System.TimeSpan) =
    let async = async ?| false
    let timeOut = timeOut ?| defaultServiceTimeOut

    match diffCallingInfo with
    | Some dci -> Main.exportDiff outputDirectory solutionName Domain.partialSolutionName async env dci.TargetEnv timeOut
    | _ -> Main.exportStandard env solutionName outputDirectory managed extended async timeOut
    
  /// <summary>Generates TypeScript context from a given environment and settings using XrmDefinitelyTyped (XDT)</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToXDT">A relative or absolute path to XrmDefinitelyTyped (XDT) executable.</param>
  /// <param name="outputDir">The directory under which the TypeScript Context will be saved.</param>
  /// <param name="solutions">List of solutions to include in TypeScript Context.</param>
  /// <param name="entities">List of entities to include in TypeScript Context.</param>
  /// <param name="extraArguments">Extra command-line arguments to pass to XrmDefinitelyTyped (XDT) executable.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member GenerateTypeScriptContext(env: Environment, pathToXDT, outputDir, ?solutions, ?entities, ?extraArguments, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let solutions = solutions ?| []
    let entities = entities ?| []
    let extraArguments = extraArguments ?| []

    Main.updateTypeScriptContext env outputDir pathToXDT logLevel solutions entities extraArguments
    
  /// <summary>Generates C# context from a given environment and settings using XrmContext</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToXrmContext">A relative or absolute path to XrmContext executable.</param>
  /// <param name="outputDir">The directory under which the C# Context will be saved.</param>
  /// <param name="solutions">List of solutions to include in C# Context.</param>
  /// <param name="entities">List of entities to include in C# Context.</param>
  /// <param name="extraArguments">Extra command-line arguments to pass to XrmContext executable.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member GenerateCSharpContext(env: Environment, pathToXrmContext, outputDir, ?solutions, ?entities, ?extraArguments, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let solutions = solutions ?| []
    let entities = entities ?| []
    let extraArguments = extraArguments ?| []

    Main.updateCustomServiceContext env outputDir pathToXrmContext logLevel solutions entities extraArguments

  /// <summary>Generates XrmMockup Metadata from a given environment and settings using XrmMockup's MetadataGenerator</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="pathToXrmMockupGenerator">A relative or absolute path to XrmMockup executable.</param>
  /// <param name="outputDir">The directory under which the XrmMockup Metadata will be saved.</param>
  /// <param name="solutions">List of solutions to include in XrmMockup Metadata.</param>
  /// <param name="entities">List of entities to include in XrmMockup Metadata.</param>
  /// <param name="extraArguments">Extra command-line arguments to pass to XrmMockup executable.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member GenerateXrmMockupMetadata(env: Environment, pathToXrmMockupGenerator, outputDir, ?solutions, ?entities, ?extraArguments, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let solutions = solutions ?| []
    let entities = entities ?| []
    let extraArguments = extraArguments ?| []

    Main.updateXrmMockupMetadata env outputDir pathToXrmMockupGenerator logLevel solutions entities extraArguments


  /// <summary>Counts the amount of entities in a solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="solutionName">The name of the solution for which to count entities.</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Count(env: Environment, solutionName, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    Main.count env solutionName logLevel


  /// <summary>Activates or deactivates all plugin steps of a solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="solutionName">The name of the solution in which to enable or disable all plugins</param>
  /// <param name="enable">Flag whether to enable or disable all solution plugins. - defaults to: true</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member EnablePluginSteps(env: Environment, solutionName, ?enable, ?logLevel) =
    Main.enablePluginSteps env solutionName enable logLevel


  /// <summary>Creates a solution in the given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="name">Desired Solution Name</param>
  /// <param name="displayName">Desired Solution Display Name</param>
  /// <param name="publisherPrefix">Desired Publisher Prefix (must be an existing publisher in the environment)</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member Create(env: Environment, name, displayName, publisherPrefix, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    Main.create env name displayName publisherPrefix logLevel


  /// <summary>Creates a publisher in the given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="name">Desired Publisher Name</param>
  /// <param name="displayName">Desired Publisher Display Name</param>
  /// <param name="prefix">Desired Publisher Prefix</param>
  /// <param name="logLevel">Log Level - Error, Warning, Info, Verbose or Debug - defaults to: 'Verbose'</param>
  static member CreatePublisher(env: Environment, name, displayName, prefix, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    Main.createPublisher env name displayName prefix logLevel

  /// <summary>Extracts a solution package using the SolutionPackager</summary>
  static member Extract(solutionFile, customizationsFolder, xmlMappingFile, projectFile, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose

    Main.extract solutionFile customizationsFolder xmlMappingFile projectFile logLevel

  /// <summary>Packs a solution package using the SolutionPackager</summary>
  static member Pack(outputFile, customizationsFolder, xmlMappingFile, ?managed, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    let managed = managed ?| false

    Main.pack outputFile customizationsFolder xmlMappingFile managed logLevel

  /// <summary>Updates the version number of a solution by the given increment, defaults to revision number</summary>
  static member UpdateVersionNumber(env: Environment, solutionName, ?increment) =
    let increment = increment ?| Revision
    
    let proxy = env.connect(log).GetService()
    
    log.Info "Updating version number of CRM solution (%A)." increment
    let solId, version = Versioning.getSolutionVersionNumber proxy solutionName
    log.Info "Current version: %A" version

    let newVersion = Versioning.incrementVersionNumber version increment
    log.Info "New version: %A" newVersion

    Versioning.updateSolutionVersionTo proxy solId newVersion
    log.Info "Version number was succesfully updated in CRM."