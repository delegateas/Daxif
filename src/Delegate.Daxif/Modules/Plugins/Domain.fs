module DG.Daxif.Modules.Plugin.Domain

open System
open System.Reflection
open Microsoft.Xrm.Sdk
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

(** Enum for custom api configurations **)
type AllowedCustomProcessingStepType =
  | None = 0
  | AsyncOnly = 1
  | SyncAndAsync = 2
  
type BindingType =
  | Global = 0
  | Entity = 1
  | EntityCollection = 2
  

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

/// Information about a Custom API definition
type Message = { 
  uniqueName: String // Cannot be changed
  name: String
  displayName: String
  description: String
  isFunction: bool // Cannot be changed
  enabledForWorkflow: int // Cannot be changed
  bindingType: int // Cannot be changed
  boundEntityLogicalName: String // Cannot be changed
  allowedCustomProcessingStepType: int // Cannot be changed
  pluginTypeName: String
  ownerId: Guid
  ownerType: String
  isCustomizable: bool
  isPrivate: bool
  executePrivilegeName: String
}

/// Information about a Custom API Request Parameter
type RequestParameter = { 
  name: string 
  uniqueName: string
  customApiName: string
  displayName: string 
  isCustomizable: bool
  isOptional: bool
  logicalEntityName: string
  _type: int
}

/// Information about a Custom API Response Property
type ResponseProperty = { 
  name: string
  uniqueName: string
  customApiName: string
  displayName: string 
  isCustomizable: bool
  logicalEntityName: string
  _type: int
}

// Information about a Custom API, its request parameters and response properties
type CustomAPI =
  { message: Message
    reqParameters: seq<RequestParameter> 
    resProperties: seq<ResponseProperty> 
  } with
  member this.TypeKey = 
    this.message.pluginTypeName
  member this.Key = 
    this.message.uniqueName
  member this.RequestParametersWithKeys =
    this.reqParameters
  member this.ResponsePropertiesWithKeys =
    this.resProperties


/// Information about an assembly
type AssemlyLocal =
  { assembly: Assembly
    assemblyId: Option<Guid>
    dllName: String
    dllPath: String
    hash: String
    isolationMode: AssemblyIsolationMode
    plugins: Plugin seq
    customAPIs: CustomAPI seq
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

type PluginConstructorType = Empty | Unsecure | Secure