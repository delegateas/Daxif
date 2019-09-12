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

// Creates an activity in CRM
let createActivity (paId : Guid) (asm : Assembly) (name : string)= 
  let version = asm.GetName().Version.ToString()
  let pt = Entity("plugintype")
  pt.Attributes.Add("name", name)
  pt.Attributes.Add("typename", name)
  pt.Attributes.Add("workflowactivitygroupname", sprintf "%s %s" (asm.GetName().Name) version)
  pt.Attributes.Add("friendlyname", Guid.NewGuid().ToString())
  pt.Attributes.Add("pluginassemblyid", EntityReference("pluginassembly", paId))
  pt.Attributes.Add("description", syncDescription())
  pt.Attributes.Add("version", version)
  pt

// Creates the found activities in the given assembly in CRM
let createActivities (proxyGen: unit -> OrganizationServiceProxy) asm paId activityNames = 
  activityNames
  |> Set.toArray
  |> Array.Parallel.iter (fun name ->
    let pluginType = createActivity paId asm name
    log.Verbose "Creating workflow activity: %s" (getName pluginType)
    proxyGen().Create pluginType |> ignore
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
let updateAssembly (proxyGen: unit -> OrganizationServiceProxy) (dllPath : string) (asm : Assembly) (pa: Entity) =
  let newContent = dllPath |> fileToBase64
  let version = asm.GetName().Version.ToString()

  let copy = Entity("pluginassembly", pa.Id)
  copy.Attributes.Add("content", newContent)
  copy.Attributes.Add("version", version)
  copy.Attributes.Add("description", syncDescription())

  copy.Attributes.Add("name", pa.["name"])
  copy.Attributes.Add("publickeytoken", pa.["publickeytoken"])
  copy.Attributes.Add("culture", pa.["culture"])
  
  proxyGen().Update(copy)
  log.Verbose "%s: %s was updated with version %s" copy.LogicalName (getName copy) version


// Checks if an existing assembly exist
// If there is one then return the id of the assembly
// If not then create a new and return the id of the newly created assembly
let instantiateAssembly (solutionId : Guid) (solutionName : string) dllName dllPath (asm: Assembly) p isolationMode = 
  log.Verbose "Retrieving assemblies from CRM"

  let ln = @"pluginassembly"
  let attrToReturn = Query.ColumnSet (true)
  let version = asm.GetName().Version.ToString()

  let pluginAssemblies = 
    CrmDataInternal.Entities.retrievePluginAssembliesByNameAndVersion p dllName version attrToReturn

  match Seq.isEmpty pluginAssemblies with
  | true -> 
    log.Verbose "No existing assembly found"
    log.Info "Creating Assembly"
    let pluginAssembly = createAssembly dllName dllPath asm isolationMode

    let pc = ParameterCollection()
    pc.Add("SolutionUniqueName", solutionName)
    let guid = CrmData.CRUD.create p pluginAssembly pc
    
    log.Verbose "%s: (%O) was created" pluginAssembly.LogicalName guid
    p.Retrieve(ln, guid, attrToReturn)

  | false -> 
    log.Verbose "Existing assembly found"

    let matchingAssembly = 
      pluginAssemblies
      |> Seq.filter (fun pa ->
        let paSolutionId = 
          pa.GetAttributeValue<AliasedValue>("solutioncomponent1.solutionid").Value :?> EntityReference

        paSolutionId.Id = solutionId)
      |> Seq.tryHead

    match matchingAssembly with
    | None -> 
      failwith "An existing assembly is found but is not registered to the solution. Register the assembly first in CRM"
    | Some x -> x

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
  let solutionId = CrmDataInternal.Entities.retrieveSolutionId p solutionName
  let pa = instantiateAssembly solutionId solutionName dllName dllPath asm p isolationMode
  let newActivities, oldActivities, targetActivities = solutionDiff asm pa.Id p

  log.WriteLine(LogLevel.Info, "Deleting workflow activities")
  deleteActivity proxyGen (oldActivities, targetActivities)
  
  log.WriteLine(LogLevel.Info, "Updating Assembly")
  updateAssembly proxyGen dllPath asm pa
  
  log.WriteLine(LogLevel.Info, "Creating workflow activities")
  createActivities proxyGen asm pa.Id newActivities
  ()
