module DG.Daxif.Modules.Plugin.Domain

open System
open System.Reflection
open DG.Daxif
open Microsoft.Xrm.Sdk

type PluginIsolationMode =
  | Sandbox = 2
  | None    = 1

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


type EventOperation = String
type LogicalName = String
type PluginTypeName = String
type StepName = String
type ImageName = String

/// Information about a plugin step
type Step =
  { pluginTypeName: PluginTypeName
    executionStage: int
    eventOperation: EventOperation
    logicalName: LogicalName
    deployment: int
    executionMode: int
    name: StepName
    executionOrder: int
    filteredAttributes: String
    userContext: Guid 
  }

/// Information about a plugin step image
type Image = 
  { stepName: StepName
    name: ImageName
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
    this.step.name
  member this.ImagesWithKeys =
    this.images
    |> Seq.map(fun image -> sprintf "%s, %s" this.StepKey image.name, image)


/// Information about a plugin assembly
type AssemlyLocal =
  { assembly: Assembly
    assemblyId: Option<Guid>
    dllName: String
    dllPath: String
    hash: String
    isolationMode: PluginIsolationMode
    plugins: Plugin seq
  }

type AssemblyRegistration = {
  id: Guid
  hash: String
} with
  static member fromEntity (e:Entity) = 
    {
      id = e.Id
      hash = e.GetAttributeValue<string>("sourcehash")
    }