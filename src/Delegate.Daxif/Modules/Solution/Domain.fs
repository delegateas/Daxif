module DG.Daxif.Modules.Solution.Domain

open System

type ImportState = 
  | NotStarted = 0
  | InProgress = 1
  | Succeeded = 2
  | Failed = 3

// Record for holding the state of an entity
type EntityState =
  { id: Guid
    logicalName: string
    stateCode: int
    statusCode: int }

// Recording holding the states of entities and which plugins and webresources to keep
// Note: the different plugin guids are not sure to be equal across environments
// so the name is used to identify the different parts of the plugins
type ExtendedSolution =
  { states: Map<string, EntityState> 
    keepAssemblies: seq<Guid*string>
    keepPluginTypes: seq<Guid*string>
    keepPluginSteps: seq<Guid*string>
    keepPluginImages: seq<Guid*string>
    keepWorkflows: seq<Guid*string>
    keepWebresources: seq<Guid*string>
  }