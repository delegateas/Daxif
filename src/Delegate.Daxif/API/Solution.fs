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
  static member PublishCustomization(env: IEnvironment, ?logLevel) =
    log.setLevelOption logLevel
    Main.PublishCustomization env

  /// <summary>Imports a solution package from a given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="diffCallingInfo">[Experimental] When specified, a diff import will be performed. Assumes exportdiff has been used prior.</param>
  static member Import(env: Environment, pathToSolutionZip, ?activatePluginSteps, ?extended, ?logLevel, ?diffCallingInfo, ?publishAfterImport) =    
    let publishAfterImport = publishAfterImport ?| true
    match diffCallingInfo with
    | Some dci -> Main.importDiff pathToSolutionZip dci.SolutionName Domain.partialSolutionName env
    | _ -> Main.importStandard env activatePluginSteps extended publishAfterImport pathToSolutionZip logLevel

  /// <summary>Exports a solution package from a given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  /// <param name="diffCallingInfo">[Experimental] When specified, a diff export happens with env as source and diffCallingInfo.TargetEnv as target</param
  static member Export(env: IEnvironment, solutionName, outputDirectory, managed, ?extended, ?deltaFromDate, ?logLevel, ?diffCallingInfo) =
    match diffCallingInfo with
    | Some dci -> Main.exportDiff outputDirectory solutionName Domain.partialSolutionName env dci.TargetEnv
    | _ -> Main.exportStandard env solutionName outputDirectory managed extended
    
  /// <summary>Generates TypeScript context from a given environment and settings using XrmDefinitelyTyped</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member GenerateTypeScriptContext(env: Environment, pathToXDT, outputDir, ?solutions, ?entities, ?extraArguments, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let solutions = solutions ?| []
    let entities = entities ?| []
    let extraArguments = extraArguments ?| []

    Main.updateTypeScriptContext env outputDir pathToXDT logLevel solutions entities extraArguments
    
  /// <summary>Generates C# context from a given environment and settings using XrmContext</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member GenerateCSharpContext(env: Environment, pathToXrmContext, outputDir, ?solutions, ?entities, ?extraArguments, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let solutions = solutions ?| []
    let entities = entities ?| []
    let extraArguments = extraArguments ?| []

    Main.updateCustomServiceContext env outputDir pathToXrmContext logLevel solutions entities extraArguments

  /// <summary>Generates XrmMockup Metadata from a given environment and settings using XrmMockup's MetadataGenerator</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member GenerateXrmMockupMetadata(env: Environment, pathToXrmMockupGenerator, outputDir, ?solutions, ?entities, ?extraArguments, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    let solutions = solutions ?| []
    let entities = entities ?| []
    let extraArguments = extraArguments ?| []

    Main.updateXrmMockupMetadata env outputDir pathToXrmMockupGenerator logLevel solutions entities extraArguments


  /// <summary>Counts the amount of entities in a solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Count(env: Environment, solutionName, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    Main.count env solutionName logLevel


  /// <summary>Activates or deactivates all plugin steps of a solution</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member EnablePluginSteps(env: Environment, solutionName, ?enable, ?logLevel) =
    Main.enablePluginSteps env solutionName enable logLevel


  /// <summary>Creates a solution in the given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member Create(env: Environment, name, displayName, publisherPrefix, ?logLevel) =
    let logLevel = logLevel ?| LogLevel.Verbose
    
    Main.create env name displayName publisherPrefix logLevel


  /// <summary>Creates a publish in the given environment</summary>
  /// <param name="env">Environment the action should be performed against.</param>
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