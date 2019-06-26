module internal DG.Daxif.Modules.Workflow.WorkflowsHelper

open System
open System.IO
open System.Reflection
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

(** Helpers functions **)
let getName (x : Entity) = x.Attributes.["name"] :?> string

let getAttribute key (e : Entity) = 
  try 
    e.Attributes.[key]
  with ex -> 
    failwith 
      ("Entity type, " + e.LogicalName + ", does not contain the attribute " + key + ". " + getFullException (ex))

let subset oldActivities (targetActivities : Entity[]) = 
  targetActivities 
  |> Array.filter (fun x ->
    oldActivities |> Seq.exists (fun s -> s = getName x)
    )
    
// Fetches the name of the workflows by finding all classes in the assembly
// that extends CodeActivity
let getActivities (asm : Assembly) = 
  getLoadableTypes asm log
  |> Array.filter (fun x -> x.BaseType <> null && x.BaseType.Name = @"CodeActivity" && x.FullName <> null)
  |> Array.map (fun x -> x.FullName)

// Creates a new assembly in CRM
let createAssembly name dll (asm : Assembly) (isolationMode: AssemblyIsolationMode) = 
  let pa = Entity("pluginassembly")
  pa.Attributes.Add("name", name)
  // DEBUG --- start ----
  pa.Attributes.Add("sourcehash", "DEBUG" |> sha1CheckSum)
  // DEBUG --- stop ----
  pa.Attributes.Add("content", dll |> fileToBase64)
  pa.Attributes.Add("isolationmode", OptionSetValue(int isolationMode)) // sandbox OptionSetValue(2)
  pa.Attributes.Add("version", asm.GetName().Version.ToString())
  pa.Attributes.Add("description", syncDescription())
  pa

// Updates an exisiting workflow assebmly in CRM
let updateAssembly' (paid : Guid) dll (asm : Assembly) = 
  let pa = Entity("pluginassembly")
  pa.Attributes.Add("pluginassemblyid", paid)
  pa.Attributes.Add("content", dll |> fileToBase64)
  pa.Attributes.Add("version", asm.GetName().Version.ToString())
  pa.Attributes.Add("description", syncDescription())
  pa

// Creates an activity in CRM
let createActivity (asmId : Guid) (asm : Assembly) (name : string) = 
  let pt = Entity("plugintype")
  pt.Attributes.Add("name", name)
  pt.Attributes.Add("typename", name)
  pt.Attributes.Add
    ("workflowactivitygroupname", sprintf "%s %s" (asm.GetName().Name) (asm.GetName().Version.ToString()))
  pt.Attributes.Add("friendlyname", Guid.NewGuid().ToString())
  pt.Attributes.Add("pluginassemblyid", EntityReference("pluginassembly", asmId))
  pt.Attributes.Add("description", syncDescription())
  pt

// Creates the found activities in the given assembly in CRM
let createActivities (proxyGen: unit -> OrganizationServiceProxy) asm assemblyId entitySet = 
  entitySet
  |> Set.toArray
  |> Array.Parallel.iter (fun name ->  
    let pluginType = createActivity assemblyId asm name
    log.Verbose "Creating workflow activity: %s" (getName pluginType)
    proxyGen().Create(pluginType) |> ignore
    log.Verbose "%s: %s was created" pluginType.LogicalName (getName pluginType))

// Deletes the exisiting activies in CRM that were not found in the assembly
let deleteActivity (proxyGen: unit -> OrganizationServiceProxy) (oldActivities, targetActivites) = 
  subset oldActivities targetActivites
  |> Seq.toArray
  |> Array.Parallel.map (fun x -> 
    proxyGen().Delete(x.LogicalName, x.Id); x
    )
  |> Array.iter (fun x -> log.Verbose "%s: %s was deleted" x.LogicalName (getName x))

// Fetches existing assembly in CRM an updates them
let updateAssembly proxyGen (dllName : string) (dllPath : string) (asm : Assembly) (solution : Entity) = 
  let p = proxyGen()
  CrmDataInternal.Entities.retrievePluginAssemblies p solution.Id
  |> Seq.filter (fun x -> dllName = getName x)
  |> Seq.iter (fun x -> 
    p.Update(updateAssembly' x.Id dllPath asm)
    log.Verbose "%s: %s was updated" x.LogicalName (getName x)
    )

// Checks if an existing assembly exist
// If there is one then return the id of the assembly
// If not then create a new and return the id of the newly created assembly
let instantiateAssembly (solutionId : Guid) (solutionName : string) dllName dllPath asm p isolationMode = 
  log.Verbose "Retrieving assemblies from CRM"
  let dlls = CrmDataInternal.Entities.retrievePluginAssembly p dllName
  match Seq.isEmpty dlls with
  | true -> 
    log.Verbose "No existing assembly found"
    log.Info "Creating Assembly"
    let pluginAssembly = createAssembly dllName dllPath asm isolationMode
    let pc = ParameterCollection()
    pc.Add("SolutionUniqueName", solutionName)
    let guid = CrmData.CRUD.create p pluginAssembly pc
    log.Verbose "%s: (%O) was created" pluginAssembly.LogicalName guid
    guid
  | false -> 
    log.Verbose "Existing assembly found"
    let matchingAssembly = 
      dlls
      |> Seq.filter (fun x -> 
        let asmSolution = 
          getAttribute "solutioncomponent1.solutionid" x :?> AliasedValue
          |> fun x -> x.Value :?> EntityReference
        asmSolution.Id = solutionId)
      |> fun x -> 
        if Seq.isEmpty x then None else x |> Seq.head |> Some

    match matchingAssembly with
    | None -> 
      failwith "An existing assembly is found but is not registered to the solution. Register the assembly first in CRM"
    | Some x -> x.Id

// Finds the different between the target assembly in CRM and the given source assembly
// Fetches the activitities in the source and target and finds the difference with
// source - target = new Activities that needs to be created
// target - source = old activities that needs to be deleted
// Returns a set of the old, new existing activities
let solutionDiff asm asmId p = 
  log.Debug "Retrieving workflow activities"
  let sourceActivities = getActivities asm
  let targetActivities = CrmDataInternal.Entities.retrievePluginTypes p asmId |> Array.ofSeq
  log.Debug "Retrieved %d plugin types" (Array.length targetActivities)

  let sourceActivities' = sourceActivities |> Set.ofArray
  let targetActivities' = 
    targetActivities
    |> Array.map getName
    |> Set.ofArray
  
  let newActivities = sourceActivities' - targetActivities'
  let oldActivities = targetActivities' - sourceActivities'
  newActivities, oldActivities, targetActivities

// Syncs the given workflow assembly to a solution in given CRM
let syncSolution' proxyGen solutionName dll isolationMode = 
  log.Verbose "Checking local assembly"
  let dll' = Path.GetFullPath(dll)
  let tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + @".dll")
  File.Copy(dll', tmp, true)
  let dllPath = Path.GetFullPath(tmp)
  log.Verbose "Connecting to CRM"
  use p = proxyGen()
  let asm = Assembly.LoadFile(dllPath)
  let dllName = Path.GetFileNameWithoutExtension(dll')
  let solution = CrmDataInternal.Entities.retrieveSolutionId p solutionName
  let asmId = instantiateAssembly solution.Id solutionName dllName dllPath asm p isolationMode
  let newActivities, oldActivities, targetActivities = solutionDiff asm asmId p
  log.WriteLine(LogLevel.Info, "Deleting workflow activities")
  deleteActivity proxyGen (oldActivities, targetActivities)
  log.WriteLine(LogLevel.Info, "Updating Assembly")
  updateAssembly proxyGen dllName dllPath asm solution
  log.WriteLine(LogLevel.Info, "Creating workflow activities")
  createActivities proxyGen asm asmId newActivities
  ()
