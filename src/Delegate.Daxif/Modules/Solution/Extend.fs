﻿module internal DG.Daxif.Modules.Solution.Extend

open System
open System.IO
open System.IO.Compression
open System.Xml
open System.Xml.Linq
open System.Xml.XPath
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Common.CrmUtility
open DG.Daxif.Modules.Serialization
open Domain
open Microsoft.Xrm.Sdk.Query

let asmLogicName = @"pluginassembly"
let typeLogicName = @"plugintype"
let stepLogicName = @"sdkmessageprocessingstep"
let imgLogicName = @"sdkmessageprocessingstepimage"
let webResLogicalName = @"webresource"
let workflowLogicalName = @"workflow"

let getName (x:Entity) = x.GetAttributeValue<string>("name")
let getOwnerRef (x:Entity) = x.GetAttributeValue<EntityReference>("ownerid")
   
// Tries to get an attribute from an entit. Fails if the attribute does not exist.
let getAttribute key (x:Entity)= 
  try
    x.Attributes.[key]
  with
  | ex -> 
    sprintf @"Entity type, %s, does not contain the attribute %s. %s. Entity Id: %s"
      (x.LogicalName) key (getFullException(ex)) (x.Id.ToString())
    |> failwith 

let getCodeValue key (x:Entity) =
  getAttribute key x :?> OptionSetValue
  |> fun e -> e.Value

let takeName = snd
let takeGuid (inp: (Guid*string)) = inp |> fst |> fun x -> x.ToString()

// Returns the solutionComponents in a sequence based on the a list of component names
let lookup (solutionComponents:seq<Guid*String>) take solutionComponentNames= 
  solutionComponentNames 
  |> Seq.map(fun name -> 
    Seq.find(fun comp -> take comp = name) solutionComponents)
  
let getEntityIds (entities: seq<Entity>) =
  entities
  |> Seq.map(fun r -> r.Id, getName r)

let getSolutionNameFromSolution solutionName (archive: ZipArchive) (log:ConsoleLogger) =
  log.Verbose "Retrieving solution.xml from solution package"
  let zipSolutionName, _ = getSolutionInformation archive

  match zipSolutionName = solutionName with
  | true  ->  solutionName
  | false ->
    log.Verbose "Solution name '%s' from solution.xml differs from solutionName argument: '%s'" zipSolutionName solutionName
    log.Verbose "Using '%s'" zipSolutionName
    zipSolutionName

// Fetches the entity of views in a packaged solution file from an exported 
// solution
let getViews p (xmlFile:string) =
   
  // Parse the customization.xml and find all the nodes containing "savedquery"
  // under the node "SavedQueries"
  let savedQueryGuids =
      let doc = new XmlDocument()
      doc.Load xmlFile
      doc.SelectNodes "//savedquery[isprivate=0 and isdefault=0]/savedqueryid/text()"
      |> Seq.cast<XmlNode> 
      |> Seq.map(fun node -> Guid.Parse node.Value)

  // Fetch the entities of the views
  savedQueryGuids
  |> Seq.map(fun guid -> CrmData.CRUD.retrieve p "savedquery" guid)

// Retrievs the workflows defined in the solution
let getWorkflows p solution =
  CrmDataInternal.Entities.retrieveWorkflows p solution

let getWebresources p solution =
  CrmDataInternal.Entities.retrieveWebResources p solution

let getUsers p =
  CrmDataInternal.Entities.retrieveSystemUsers p

let getDomainnameUser (p: IOrganizationService) (ref: EntityReference) =
  p.Retrieve("systemuser", ref.Id, ColumnSet("domainname")).GetAttributeValue<string>("domainname")

let getEntityIdsAndOwners proxy (entities: seq<Entity>) =
  entities
  |> Seq.map(fun e -> e.Id, getName e, getOwnerRef e |> getDomainnameUser proxy)

// Retrievs the assemblies, types, active steps, and images of a solution
let getPluginsIds p solution =
    
  // Find all assemblies in the solution
  let assemblies = 
    CrmDataInternal.Entities.retrievePluginAssemblies p solution
  let asmName = 
    assemblies
    |> Seq.map(fun x -> x.Id, getName x)

  // Find all types in the solution
  let types = 
    assemblies
    |> Seq.toArray
    |> Array.Parallel.map(fun asm -> CrmDataInternal.Entities.retrievePluginTypes p asm.Id)
    |> Array.toSeq
    |> Seq.concat
    |> Seq.map(fun x -> x.Id, getName x)

  // Find all active Steps
  let steps = 
    CrmDataInternal.Entities.retrieveAllPluginProcessingSteps p solution
    |> Seq.filter(fun e -> 
      (getCodeValue "statuscode" e) = 1 &&
      (getCodeValue "statecode" e) = 0) 

  let stepsName = 
    steps
    |> Seq.map(fun x -> 
      let stage = getAttribute "stage" x :?> OptionSetValue
      x.Id, (getName x))

  // Find all images of active steps 
  // Note: Use 
  let images = 
    steps
    |> Seq.toArray
    |> Array.Parallel.map(fun step ->
      CrmDataInternal.Entities.retrievePluginProcessingStepImages p step.Id
      |> Seq.map(fun x -> x.Id, getName step + " " + getName x))
    |> Array.toSeq
    |> Seq.concat

  asmName, types, stepsName, images

let deactivateWorkflows p ln (diff: (Guid*String)[]) (log:ConsoleLogger) =
  match diff.Length with 
  | 0 -> ()
  | _ ->
    diff
    |> Array.map (fun (id,name) ->
      CrmDataInternal.Entities.updateStateReq ln id 0 1 :> OrganizationRequest
    )
    |> CrmDataInternal.CRUD.performAsBulkWithOutput p log


// Stores entities statecode and statuscode in a seperate file to be
// implemented on import
let export service solution solutionPath =
  log.Info @"Exporting extended solution"
  let solutionId = CrmDataInternal.Entities.retrieveSolutionId service solution

  // Retriev Customization.xml file from the solution package and store it in 
  // a temp folder
  let tempFolder = InternalUtility.createTempFolder()

  log.WriteLine(LogLevel.Verbose, @"Extracting customization.xml from solution")

  use zipToOpen = new FileStream(solutionPath, FileMode.Open)
  use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)
  let xmlFile = Path.Combine(tempFolder, "customizations.xml")
  let entry = archive.GetEntry("customizations.xml")
  entry.ExtractToFile(xmlFile)

  log.WriteLine(LogLevel.Verbose, "Finding entities to be persisted")

  // find the entities to be persisted
  let views = getViews service xmlFile
  let workflows = getWorkflows service solutionId

  let entities =
    [ ("Views",views)
      ("Workflows",workflows)]
    |> List.toSeq

  entities
  |> Seq.iter(fun (name,x) -> 
    log.WriteLine(LogLevel.Verbose, sprintf "Found %d %s" (Seq.length x) name))

  // Add state and statuscodes to a delegateSolution record
  let states =
    entities
    |> Seq.map snd
    |> Seq.concat
    |> Seq.filter(fun entity -> 
      entity.Attributes.ContainsKey "statuscode" && 
      entity.Attributes.ContainsKey "statecode")
    |> Seq.map(fun entity ->
      let guidState = 
        { id = entity.Id
          logicalName = entity.LogicalName
          stateCode = getCodeValue "statecode" entity
          statusCode = getCodeValue "statuscode" entity}
      (entity.Id.ToString(), guidState))
    |> Map.ofSeq

  log.WriteLine(LogLevel.Verbose, "Finding plugins to be persisted")

  // Find assemblies, plugin types, active plugin steps, and plugin images 
  let asmsIds, typesIds, stepsIds, imgsIds = getPluginsIds service solutionId

  [|("Assemblies", asmsIds); ("Plugin Types", typesIds) 
    ("Plugin Steps", stepsIds); ("Step Images", imgsIds)|]
  |> Array.iter(fun (name, x) -> 
    log.WriteLine(LogLevel.Verbose, sprintf @"Found %d %s" (Seq.length x) name )
    )

  let workflowsIdsAndOwners = workflows |> getEntityIdsAndOwners service
  let webResIds = getWebresources service solutionId |> getEntityIds

  let delegateSolution = 
    { states=states
      keepAssemblies = asmsIds
      keepPluginTypes = typesIds
      keepPluginSteps = stepsIds
      keepPluginImages = imgsIds
      keepWorkflows = workflowsIdsAndOwners
      keepWebresources = webResIds}
    
  log.WriteLine(LogLevel.Verbose, @"Creating extended solution file")

  // Serialize the record to an xml file called ExtendedSolution.xml and add it to the
  // packaged solution 
  let arr = 
    SerializationHelper.serializeXML<ExtendedSolution> delegateSolution
    |> SerializationHelper.xmlPrettyPrinterHelper'

  let solEntry = archive.CreateEntry("ExtendedSolution.xml")
  use writer = new StreamWriter(solEntry.Open())
  writer.BaseStream.Write(arr,0,arr.Length)

  log.WriteLine(LogLevel.Verbose, sprintf @"Added %s to solution package" solEntry.Name)

  Directory.Delete(tempFolder,true)

  log.WriteLine(LogLevel.Info, @"Extended solution exported successfully")

let getExtendedSolutionAndId (service: IOrganizationService) solutionName zipPath = 
  use zipToOpen = new FileStream(zipPath, FileMode.Open)
  use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)

  log.Verbose @"Attempting to retrieve ExtendedSolution.xml file from solution package"
  match archive.Entries |> Seq.exists(fun e -> e.Name = "ExtendedSolution.xml") with                                        
  | false -> 
    failwith @"ExtendedSolution import failed. No ExtendedSolution.xml file found in solution package"

  | true -> 
    let entry = archive.GetEntry("ExtendedSolution.xml")
    use writer = new StreamReader(entry.Open())

    let xmlContent = writer.ReadToEnd()
      
    let extSol = SerializationHelper.deserializeXML<ExtendedSolution> xmlContent
    
    let zipSolName = getSolutionNameFromSolution solutionName archive log
    let solutionId = CrmDataInternal.Entities.retrieveSolutionId service zipSolName
      
    solutionId,extSol


let tryGetExtendedSolutionAndId (service: IOrganizationService) solutionName zipPath =
  try
    getExtendedSolutionAndId service solutionName zipPath |> Some
  with _ -> None


let deleteElements service elementGroup =
  let ln, source, target, fieldCompFunc, preDeleteAction = elementGroup
  let sourceIdentifiers = source |> Seq.map fieldCompFunc |> Set.ofSeq
  let diff = 
    target
    |> Seq.filter(fun (id,name) ->
      let identifier = fieldCompFunc (id,name)
      sourceIdentifiers.Contains identifier |> not
    )
    |> Array.ofSeq
      
  match preDeleteAction with
  | None   -> ()
  | Some action -> action service ln diff log
        
  log.Verbose "Found %d '%s' entities to be deleted " diff.Length ln
      
  match diff.Length with 
  | 0 -> true
  | _ ->
    diff 
    |> Array.map (fun (id,name) ->
      log.Verbose "Deleting '%s' with name '%s' and GUID '%s'" ln name (id.ToString())
      CrmData.CRUD.deleteReq ln id :> OrganizationRequest
    )
    |> fun req -> 
      try 
        CrmDataInternal.CRUD.performAsBulkWithOutput service log req
        true
      with _ -> 
        false;


let preImport service solutionName zipPath =
  log.Info @"Performing pre-steps for importing extended solution"
  match tryGetExtendedSolutionAndId service solutionName zipPath with
  | None -> 
    log.Verbose "No solution exists yet, skipping pre steps"
    ()
  | Some(solutionId,extSol) -> 
    let mutable errors = false in
    // Sync plugins and workflows
    let targetAsms, targetTypes, targetSteps, targetImgs = getPluginsIds service solutionId
    let targetWorkflows = getWorkflows service solutionId |> getEntityIds
    let sourceWorkflows = extSol.keepWorkflows |> Seq.map (fun (id,name,_) -> id,name)
    
    let deletionError =
      [|(imgLogicName, extSol.keepPluginImages, targetImgs, takeGuid, None)
        (stepLogicName, extSol.keepPluginSteps, targetSteps, takeGuid, None)
        (typeLogicName, extSol.keepPluginTypes, targetTypes, takeName, None)
        (asmLogicName, extSol.keepAssemblies, targetAsms, takeName, None)
        (workflowLogicalName, sourceWorkflows, targetWorkflows, takeGuid, Some(deactivateWorkflows))|]
      |> Array.map (fun x -> deleteElements service x)
      |> Array.exists (fun x -> not x)

    if deletionError then errors <- true

    if errors then
      failwith "There were errors during the pre-steps of extended solution"
    else
      log.WriteLine(LogLevel.Info, @"Extend pre-steps completed")

let postImport service solutionName zipPath reassignWorkflows =
  log.Info @"Performing post-steps for importing extended solution"
  let solutionId, extSol = getExtendedSolutionAndId service solutionName zipPath
  
  let mutable errors = false in

  // Attempt to find owners in target to preserve owners of workflows
  if reassignWorkflows then
    log.Verbose "Ensuring correct owner on workflows"
  
    let workflowOwners =
      extSol.keepWorkflows
      |> Seq.map (fun (_,_,domainname) -> domainname)
      |> Seq.distinct
      |> Set.ofSeq
  
    let userMapping = 
      getUsers service
      |> Array.ofSeq
      |> Array.Parallel.map (fun e -> e.GetAttributeValue<string>("domainname"),e.Id)
      |> Array.filter (fun (domainname,_) -> workflowOwners.Contains domainname)
      |> Map.ofArray
  
    let wfToUpdate =
      extSol.keepWorkflows
      |> Array.ofSeq
      |> Array.filter (fun (_,_,domainname) -> userMapping.ContainsKey domainname)

    log.Verbose "Found %i worfklows that could be reassigned" wfToUpdate.Length

    log.Verbose "Setting workflows to draft"
    wfToUpdate
    |> Array.map(fun (id,_,_) -> 
      CrmDataInternal.Entities.updateStateReq "workflow" id 0 -1 :> OrganizationRequest )
    |> fun req -> 
      try CrmDataInternal.CRUD.performAsBulkWithOutput service log req
      with _ -> errors <- true;

    log.Verbose "Reassigning workflows"
    wfToUpdate
    |> Array.map(fun (id,_,domainname) ->
      CrmDataInternal.Entities.assignReq userMapping.[domainname] "workflow" id :> OrganizationRequest )
    |> fun req -> 
      try CrmDataInternal.CRUD.performAsBulkWithOutput service log req
      with _ -> errors <- true;


  // Read the status and statecode of the entities and update them in crm
  log.Verbose @"Finding states of entities to be updated"

  // Find the source entities that have different code values than target
  let diffExtSol =
    extSol.states
    |> Map.toArray
    |> Array.Parallel.map(fun (_,guidState) -> 
        CrmData.CRUD.retrieveReq guidState.logicalName guidState.id 
        :> OrganizationRequest )
    |> CrmDataHelper.performAsBulk service
    |> Array.Parallel.map(fun resp -> 
      let resp' = resp.Response :?> Messages.RetrieveResponse 
      resp'.Entity)
    |> Array.filter(fun target -> 
      let source = extSol.states.[target.Id.ToString()]
      getCodeValue "statecode" target <> source.stateCode ||
      getCodeValue "statuscode" target <> source.statusCode)
      
  log.Verbose @"Found %d entity states to be updated" diffExtSol.Length

  // Update the entities states
  match diffExtSol |> Seq.length with
  | 0 -> ()
  | _ ->
    log.WriteLine(LogLevel.Verbose, @"Updating entity states")
    diffExtSol
    |> Array.map(fun x -> 
      let x' = extSol.states.[x.Id.ToString()]
      CrmDataInternal.Entities.updateStateReq x'.logicalName x'.id
        x'.stateCode x'.statusCode :> OrganizationRequest )
    |> fun req -> 
      try CrmDataInternal.CRUD.performAsBulkWithOutput service log req
      with _ -> errors <- true;

  log.Verbose "Synching plugins"

  // Sync Webresources
  let targetWebRes = getWebresources service solutionId |> getEntityIds

  let deletionError =
    [|(webResLogicalName, extSol.keepWebresources, targetWebRes, takeGuid, None)|]
    |> Array.map (fun x -> deleteElements service x)
    |> Array.exists (fun x -> not x)

  if deletionError then errors <- true

  if errors then
    failwith "There were errors during the post-steps of extended solution"
  else
    log.WriteLine(LogLevel.Info, @"Extend post-steps completed")
