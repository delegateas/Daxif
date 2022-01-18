﻿module DG.Daxif.Modules.Solution.Domain

open System
open System.Reflection
open Microsoft.Xrm.Sdk
open AsyncJobHelper

let partialSolutionName = "DAXIFPartialSolution"

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
    keepWorkflows: seq<Guid*string*string>
    keepWebresources: seq<Guid*string>
    keepCustomAPIs: seq<Guid*string>
  }

type ImportJobInfo =
  {
    solution: string
    managed: bool
    jobId: Guid
    asyncJobId: Option<Guid>
    status: AsyncJobStatus
    progress: double
    result: Option<JobResult>
    excelFile: Option<string>
  }

type ExportAsyncJobInfo =
  {
    solution: string
    managed: bool
    jobId: Guid
    asyncJobId: Guid
    status: AsyncJobStatus
    result: Option<JobResult>
  }

type EntityComponent = 
  | EntityMetaData = 1
  | Attribute = 2
  | View = 26
  | Chart = 59
  | Form = 60

type SolutionComponent = 
  | Entity = 1
  | Ribbon = 1
  | OptionSet = 9
  | EntityRelationship = 10
  | Role = 20
  | Workflow = 29
  | Dashboard = 60 // Dashboard has the same id as form!
  | WebResource = 61
  | SiteMap = 62
  | FieldSecurityProfile = 70
  | AppModule = 80
  | PluginAssembly = 91
  | PluginStep = 92
