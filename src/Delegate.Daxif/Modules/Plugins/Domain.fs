module DG.Daxif.Modules.Plugin.Domain

open System
open System.Reflection
open DG.Daxif


(** Enum for plugin configurations **)
type ExecutionMode = 
  | Synchronous = 0
  | Asynchronous = 1

type ExecutionStage = 
  | PreValidation = 10
  | Pre = 20
  | Post = 40

type Deployment =
  | ServerOnly = 0
  | MicrosoftDynamicsCRMClientforOutlookOnly = 1
  | Both = 2

type ImageType =
  | PreImage = 0
  | PostImage = 1
  | Both = 2

/// Information about a plugin step
type Step =
  { pluginTypeName: String
    executionStage: int
    eventOperation: String
    logicalName: String
    deployment: int
    executionMode: int
    name: String
    executionOrder: int
    filteredAttributes: String
    userContext: Guid 
  } with
  member this.messageName = 
    let entity' = String.IsNullOrEmpty(this.logicalName) |> function
        | true -> "any Entity" | false -> this.logicalName
    let execMode = (enum<ExecutionMode> this.executionMode).ToString()
    let execStage = (enum<ExecutionStage> this.executionStage).ToString()
    sprintf "%s: %s %s %s of %s" this.pluginTypeName execMode execStage this.eventOperation entity'

/// Information about a plugin step image
type Image = 
  { stepName: string
    name: string
    entityAlias: string
    imageType: int
    attributes: string 
  }


// Information about a plugin, its step and images
type Plugin =
  { step: Step
    images: seq<Image> 
  } with
  member this.TypeKey = 
    this.step.pluginTypeName
  member this.StepKey = 
    this.step.messageName
  member this.ImagesWithKeys =
    this.images
    |> Seq.map(fun image -> sprintf "%s, %s" this.StepKey image.name, image)


/// Information about a plugin assembly
type AssemblyContext =
  { assembly: Assembly
    assemblyId: Option<Guid>
    dllName: String
    dllPath: String
    hash: String
    isolationMode: PluginIsolationMode
    plugins: Plugin seq
  }
