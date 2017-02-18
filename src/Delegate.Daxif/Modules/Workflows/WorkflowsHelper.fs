module internal DG.Daxif.Modules.Workflow.WorkflowsHelper

open System
open System.IO
open System.Reflection
open System.Xml.Linq
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility


(** Helpers functions **)
let getName (x:Entity) = x.Attributes.["name"] :?> string

let getAttribute key (x:Entity) = 
  try
    x.Attributes.[key]
  with
  | ex -> 
    failwith 
      ("Entity type, " + x.LogicalName + 
      ", does not contain the attribute " 
      + key + ". " + getFullException(ex));

let subset a (b:Entity seq) = 
  b |> Seq.filter(fun x -> 
    let z = getName x
    ((fun y -> y = z),a) ||> Seq.exists)

// Fetches the name of the workflows by finding all classes in the assembly
// that extends CodeActivity
let getActivities (asm:Assembly) =
  asm.GetTypes() |> fun xs -> 
    xs 
    |> Array.filter (fun x -> 
      x.BaseType <> null && 
      x.BaseType.Name = @"CodeActivity" &&
      x.FullName <> null)
    |> Array.map(fun x -> x.FullName)
    |> Array.toSeq

// Creates a new assembly in CRM
let createAssembly name dll (asm:Assembly) =
  let pa = Entity("pluginassembly")
  pa.Attributes.Add("name", name)
  // DEBUG --- start ----
  pa.Attributes.Add("sourcehash", "DEBUG" |> sha1CheckSum)
  // DEBUG --- stop ----
  pa.Attributes.Add("content", dll |> fileToBase64)
  pa.Attributes.Add("isolationmode", OptionSetValue(2)) // sandbox
  pa.Attributes.Add("version", asm.GetName().Version.ToString())
  pa.Attributes.Add("description", syncDescription())
  pa

// Updates an exisiting workflow assebmly in CRM
let updateAssembly' (paid:Guid) dll (asm:Assembly) =
  let pa = Entity("pluginassembly")
  pa.Attributes.Add("pluginassemblyid", paid)
  pa.Attributes.Add("content", dll |> fileToBase64)
  pa.Attributes.Add("version", asm.GetName().Version.ToString())
  pa.Attributes.Add("description", syncDescription())
  pa

// Creates an activity in CRM
let createActivity (asmId:Guid) (asm:Assembly) (name:string) =
  let pt = Entity("plugintype")
  pt.Attributes.Add("name", name)
  pt.Attributes.Add("typename", name)
  pt.Attributes.Add("workflowactivitygroupname", 
    sprintf "%s %s" (asm.GetName().Name) (asm.GetName().Version.ToString()))
  pt.Attributes.Add("friendlyname", Guid.NewGuid().ToString())
  pt.Attributes.Add("pluginassemblyid", EntityReference("pluginassembly",asmId))
  pt.Attributes.Add("description", syncDescription())
  pt

// Creates the found activities in the given assembly in CRM
let createActivities asm assemblyId (log:ConsoleLogger) m tc entitySet =
    
  entitySet 
  |> Set.toArray
  |> Array.Parallel.iter(fun x ->
    ServiceProxy.proxyContext m tc (fun p -> 
      let pt = createActivity assemblyId asm x

      log.WriteLine(LogLevel.Verbose, 
        sprintf "Creating workflow activity: %s" (getName pt))
      (pt, CrmData.CRUD.create p pt (ParameterCollection()))
      |> fun (x,_) -> 
        log.WriteLine(LogLevel.Verbose, 
          sprintf "%s: %s was created" x.LogicalName (getName x))))
  
// Deletes the exisiting activies in CRM that were not found in the assembly
let deleteActivity (log:ConsoleLogger) m tc entitySet = 
  entitySet ||> subset
  |> Seq.toArray
  |> Array.Parallel.map(fun x ->
      
    ServiceProxy.proxyContext m tc (fun p -> 
      x, CrmData.CRUD.delete p x.LogicalName x.Id))
  |> Array.iter(
    fun (x,_) -> 
      log.WriteLine(LogLevel.Verbose,
        sprintf "%s: %s was deleted" x.LogicalName (getName x)))

// Fetches existing assembly in CRM an updates them
let updateAssembly (log:ConsoleLogger) (dllName:string) 
  (dllPath:string) (asm:Assembly) m tc (solution:Entity) = 
    ServiceProxy.proxyContext m tc (fun p -> 
      let dlls = CrmDataInternal.Entities.retrievePluginAssemblies p solution.Id

      dlls
      |> Seq.filter(fun x -> dllName = getName x)
      |> Seq.iter(fun x ->
         
        CrmData.CRUD.update
            p (updateAssembly' x.Id dllPath asm) |> ignore

        log.WriteLine(LogLevel.Verbose,
          sprintf "%s: %s was updated" x.LogicalName (getName x))))

// Checks if an existing assembly exist
// If there is one then return the id of the assembly
// If not then create a new and return the id of the newly created assembly
let instantiateAssembly (solution:Entity) dllName dllPath asm p 
  (log:ConsoleLogger) =
    log.WriteLine(LogLevel.Verbose, "Retrieving assemblies from CRM")
    let dlls = CrmDataInternal.Entities.retrievePluginAssembly p dllName

    match Seq.isEmpty dlls with
    | true -> 
      log.WriteLine(LogLevel.Verbose, "No existing assembly found")
      log.WriteLine(LogLevel.Info, "Creating Assembly")
      let pa = createAssembly dllName dllPath asm
      let pc = ParameterCollection()

      pc.Add("SolutionUniqueName", getAttribute "uniquename" solution)
      let guid = CrmData.CRUD.create p pa pc

      log.WriteLine(LogLevel.Verbose,
          sprintf "%s: (%O) was created" pa.LogicalName guid)
      guid
    | false -> 
      log.WriteLine(LogLevel.Verbose, "Existing assembly found")
      let matchingAssembly = 
        dlls
        |> Seq.filter(fun x -> 
          let asmSolution = 
            x
            |> fun x -> getAttribute "solutioncomponent1.solutionid" x :?> AliasedValue
            |> fun x -> x.Value :?> EntityReference
          asmSolution.Id = solution.Id)
        |> fun x -> if Seq.isEmpty x then None else x |> Seq.head |> Some

      match matchingAssembly with
      | None -> 
        failwith "An existing assembly is found but is not registered to the solution. Register the assembly first in CRM"
      | Some x -> 
        x.Id

// Finds the different between the target assembly in CRM and the given source assembly
// Fetches the activitities in the source and target and finds the difference with
// source - target = new Activities that needs to be created
// target - source = old activities that needs to be deleted
// Returns a set of the old, new existing activities
let solutionDiff asm asmId p (log:ConsoleLogger) = 

  log.WriteLine(LogLevel.Debug, "Retrieving workflow activities")

  let sourceActivities = getActivities asm
  let targetActivities = CrmDataInternal.Entities.retrievePluginTypes p asmId

  log.WriteLine(LogLevel.Debug, 
    sprintf "Retrieved %d plugin types" (Seq.length targetActivities))

  let sourceActivities' = 
    sourceActivities
    |> Set.ofSeq
  let targetActivities' = 
    targetActivities
    |> Seq.map getName
    |> Set.ofSeq

  let newActivities  = sourceActivities' - targetActivities'
  let oldActivities  = targetActivities' - sourceActivities'
  newActivities, oldActivities, targetActivities

// Syncs the given workflow assembly to a solution in given CRM
let syncSolution' org ac solutionName dll (log:ConsoleLogger) =
  log.WriteLine(LogLevel.Verbose, "Checking local assembly")
  let dll'  = Path.GetFullPath(dll)
  let tmp   = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString() + @".dll")

  File.Copy(dll',tmp,true)

  let dllPath = Path.GetFullPath(tmp)
    
  log.WriteLine(LogLevel.Verbose, "Connecting to CRM")
  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc
  let asm = Assembly.LoadFile(dllPath); 
  let dllName = Path.GetFileNameWithoutExtension(dll'); 
  let solution = CrmDataInternal.Entities.retrieveSolution p solutionName

  let asmId = instantiateAssembly solution dllName dllPath asm p log

  let newActivities, oldActivities, targetActivities = 
    solutionDiff asm asmId p log

  log.WriteLine(LogLevel.Info, "Deleting workflow activities")
  deleteActivity log m tc (oldActivities,targetActivities) 

  log.WriteLine(LogLevel.Info, "Updating Assembly")
  updateAssembly log dllName dllPath asm m tc solution

  log.WriteLine(LogLevel.Info, "Creating workflow activities")
  createActivities asm asmId log m tc newActivities

  ()