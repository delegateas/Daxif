namespace DG.Daxif.HelperModules

open System
open System.IO
open System.IO.Compression
open System.Xml
open System.Xml.Linq
open System.Xml.XPath
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility
open DG.Daxif.HelperModules.Common.ConsoleLogger

module internal DGSolutionHelper =

  // Record for holding the state of an entity
  type EntityState =
    { id: Guid
      logicalName: string
      stateCode: int
      statusCode: int }

  // Recording holding the states of entities and which plugins and webresources to keep
  // Note: the different plugin guids are not sure to be equal across environments
  // so the name is used to identify the different parts of the plugins
  type DelegateSolution =
    { states: Map<string, EntityState> 
      keepAssemblies: seq<Guid*string>
      keepPluginTypes: seq<Guid*string>
      keepPluginSteps: seq<Guid*string>
      keepPluginImages: seq<Guid*string>
      keepWorkflows: seq<Guid*string>
      keepWebresources: seq<Guid*string>}

  let asmLogicName = @"pluginassembly"
  let typeLogicName = @"plugintype"
  let stepLogicName = @"sdkmessageprocessingstep"
  let imgLogicName = @"sdkmessageprocessingstepimage"
  let webResLogicalName = @"webresource"
  let workflowLogicalName = @"workflow"

  let getName (x:Entity) = x.Attributes.["name"] :?> string
   
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

  // Returns a subset of the sequence based on the comparison on the a strings
  let subset (b:seq<Guid*String>) a = 
    b |> Seq.filter(fun (_,z) -> 
      ((fun y -> y = z),a) ||> Seq.exists)

  let getEntityIds (entities: seq<Entity>) =
    entities
    |> Seq.map(fun r -> r.Id, getName r)

  let takeName = snd
  let takeGuid (inp: (Guid*string)) = inp |> fst |> fun x -> x.ToString()

  let getSolutionNameFromSolution solutionName (archive: ZipArchive) (log :ConsoleLogger.ConsoleLogger) =
    log.WriteLine(LogLevel.Verbose, 
      @"Retrieving solution.xml from solution package")
    match archive.Entries |> Seq.exists(fun e -> e.Name = "solution.xml") with
    | false -> 
      failwith "Incorrect CRM package. solution.xml file not found"
      solutionName
    | true -> 
      let solutionNameNode = 
        archive.GetEntry("solution.xml")
        |> fun entry -> XDocument.Load(entry.Open())
        |> fun xd -> xd.XPathSelectElement("/ImportExportXml/SolutionManifest/UniqueName");
      match not solutionNameNode.IsEmpty, solutionNameNode.Value <> solutionName with
      | false, _ ->  
        failwith "Incorrect CRM package. Solution name not found in package"
        solutionName
      | true, false -> 
        solutionName
      | true, true -> 
        let zipSolName = solutionNameNode.Value
        log.WriteLine(LogLevel.Verbose, 
          sprintf "Solution name '%s' from solution.xml differs from solutionName argument: '%s'" zipSolName solutionName);
        log.WriteLine(LogLevel.Verbose, sprintf "Using '%s'" zipSolName);
        zipSolName
      

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

  let deactivateWorkflows p ln target (diff: Set<string>) log =
    match diff.Count with
    | 0 -> ()
    | _ -> 
      diff
      |> Set.toSeq
      |> subset target
      |> Seq.map( fun (x,_) ->
        CrmDataInternal.Entities.updateStateReq ln x 0 1 :> OrganizationRequest )
      |> Seq.toArray
      |> CrmDataInternal.CRUD.performAsBulkWithOutput p log


  // Stores entities statecode and statuscode in a seperate file to be
  // implemented on import
  let exportDGSolution org ac solutionName zipPath (log:ConsoleLogger) =

    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc

    let solution = CrmDataInternal.Entities.retrieveSolution p solutionName

    // Retriev Customization.xml file from the solution package and store it in 
    // a temp folder
    let tempFolder = createTempFolder ()

    log.WriteLine(LogLevel.Verbose, @"Extracting customization.xml from solution")

    use zipToOpen = new FileStream(zipPath, FileMode.Open)
    use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)
    let xmlFile = Path.Combine(tempFolder, "customizations.xml")
    let entry = archive.GetEntry("customizations.xml")
    entry.ExtractToFile(xmlFile)

    log.WriteLine(LogLevel.Verbose, "Finding entities to be persisted")

    // find the entities to be persisted
    let views = getViews p xmlFile
    let workflows = getWorkflows p solution.Id

    let entities =
      [("Views",views)
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
          {id = entity.Id
           logicalName = entity.LogicalName
           stateCode = getCodeValue "statecode" entity
           statusCode = getCodeValue "statuscode" entity}
        (entity.Id.ToString(), guidState))
      |> Map.ofSeq

    log.WriteLine(LogLevel.Verbose, "Finding plugins to be persisted")

    // Find assemblies, plugin types, active plugin steps, and plugin images 
    let asmsIds, typesIds, stepsIds, imgsIds = getPluginsIds p solution.Id

    [|("Assemblies", asmsIds); ("Plugin Types", typesIds) 
      ("Plugin Steps", stepsIds); ("Step Images", imgsIds)|]
    |> Array.iter(fun (name, x) -> 
      log.WriteLine(LogLevel.Verbose, sprintf @"Found %d %s" (Seq.length x) name )
      )

    let workflowsIds = workflows |> getEntityIds
    let webResIds = getWebresources p solution.Id |> getEntityIds

    let delegateSolution = 
      {states=states
       keepAssemblies = asmsIds
       keepPluginTypes = typesIds
       keepPluginSteps = stepsIds
       keepPluginImages = imgsIds
       keepWorkflows = workflowsIds
       keepWebresources = webResIds}
    
    log.WriteLine(LogLevel.Verbose, @"Creating solution file")

    // Serialize the record to an xml file called dgSolution.xml and add it to the
    // packaged solution 
    let arr = 
      SerializationHelper.serializeXML<DelegateSolution> delegateSolution
      |> SerializationHelper.xmlPrettyPrinterHelper'

    let solEntry = archive.CreateEntry("dgSolution.xml")
    use writer = new StreamWriter(solEntry.Open())
    writer.BaseStream.Write(arr,0,arr.Length)

    log.WriteLine(LogLevel.Verbose, sprintf @"Added %s to solution package" solEntry.Name)

    Directory.Delete(tempFolder,true)

    log.WriteLine(LogLevel.Info, @"DGSolution exported successfully")

  let importDGSolution org ac solutionName zipPath (log:ConsoleLogger) =
    use zipToOpen = new FileStream(zipPath, FileMode.Open)
    use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)

    let zipSolName = getSolutionNameFromSolution solutionName archive log

    log.WriteLine(LogLevel.Verbose, 
      @"Attempting to retrieve dgSolution.xml file from solution package")
    match archive.Entries |> Seq.exists(fun e -> e.Name = "dgSolution.xml") with                                        
    | false -> 
      failwith @"DGSolution import fialed. No stored dgSolution.xml file found in package"
    | true -> 
      let m = ServiceManager.createOrgService org
      let tc = m.Authenticate(ac)
      use p = ServiceProxy.getOrganizationServiceProxy m tc
      let solution = CrmDataInternal.Entities.retrieveSolution p zipSolName

      // Fetch the dgSolution.xml file and unserialize it
      let entry = archive.GetEntry("dgSolution.xml")
      use writer = new StreamReader(entry.Open())

      let xmlContent = writer.ReadToEnd()
      
      let dgSol = SerializationHelper.deserializeXML<DelegateSolution> xmlContent
      
      // Read the status and statecode of the entities and update them in crm
      log.WriteLine(LogLevel.Verbose, @"Finding states of entities to be updated")

      // Find the source entities that have different code values than target
      let diffdgSol =
        dgSol.states
        |> Map.toArray
        |> Array.Parallel.map(fun (_,guidState) -> 
           CrmData.CRUD.retrieveReq guidState.logicalName guidState.id 
           :> OrganizationRequest )
        |> CrmData.CRUD.performAsBulk p
        |> Array.Parallel.map(fun resp -> 
          let resp' = resp.Response :?> Messages.RetrieveResponse 
          resp'.Entity)
        |> Array.filter(fun target -> 
          let source = dgSol.states.[target.Id.ToString()]
          getCodeValue "statecode" target <> source.stateCode ||
          getCodeValue "statuscode" target <> source.statusCode)

      
      log.WriteLine(LogLevel.Verbose, 
        sprintf @"Found %d entity states to be updated" diffdgSol.Length )

      // Update the entities states
      match diffdgSol |> Seq.length with
      | 0 -> ()
      | _ ->
        log.WriteLine(LogLevel.Verbose, @"Updating entity states")
        diffdgSol
        |> Array.map(fun x -> 
          let x' = dgSol.states.[x.Id.ToString()]
          CrmDataInternal.Entities.updateStateReq x'.logicalName x'.id
            x'.stateCode x'.statusCode :> OrganizationRequest )
        |> CrmDataInternal.CRUD.performAsBulkWithOutput p log

      log.WriteLine(LogLevel.Verbose, "Synching plugins")

      // Sync Plugins and Webresources
      let targetAsms, targetTypes, targetSteps, targetImgs = getPluginsIds p solution.Id
      let targetWorkflows = getWorkflows p solution.Id |> getEntityIds
      let targetWebRes = getWebresources p solution.Id |> getEntityIds

      [|(imgLogicName, dgSol.keepPluginImages, targetImgs, takeGuid, None)
        (stepLogicName, dgSol.keepPluginSteps, targetSteps, takeGuid, None)
        (typeLogicName, dgSol.keepPluginTypes, targetTypes, takeName, None)
        (asmLogicName, dgSol.keepAssemblies, targetAsms, takeGuid, None)
        (webResLogicalName, dgSol.keepWebresources, targetWebRes, takeGuid, None)
        (workflowLogicalName, dgSol.keepWorkflows, targetWorkflows, takeGuid, Some(deactivateWorkflows))|]
      |> Array.iter(fun (ln, source, target, fieldCompFunc, func) ->   
        
        let s = source |> Seq.map fieldCompFunc |> Set.ofSeq
        let t = target |> Seq.map fieldCompFunc |> Set.ofSeq
        let diff = t - s

        match func with
        | None -> ()
        | Some(f) -> f p ln target diff log
        
        log.WriteLine(LogLevel.Verbose, sprintf "Found %d '%s' entities to be deleted " diff.Count ln)
        match diff.Count with
        | 0 -> ()
        | _ -> 
          diff
          |> Set.toSeq
          |> subset target
          |> Seq.map( fun (x,_) ->
            CrmData.CRUD.deleteReq ln x :> OrganizationRequest)
          |> Seq.toArray
          |> CrmDataInternal.CRUD.performAsBulkWithOutput p log
        )
