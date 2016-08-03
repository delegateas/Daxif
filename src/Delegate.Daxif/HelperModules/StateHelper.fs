namespace DG.Daxif.HelperModules

open System
open System.IO
open System.IO.Compression
open System.Xml
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility
open DG.Daxif.HelperModules.Common.ConsoleLogger



module internal StateHelper =

  type GuidState =
    { id: Guid
      logicalName: string
      stateCode: int
      statusCode: int }

  type DelegateSolution =
    { states: Map<string, GuidState> }
    
  // Tries to get an attribute from an entit. Fails if the attribute does not exist.
  let getAttribute key (x:Entity) (log:ConsoleLogger) = 
    try
      x.Attributes.[key]
    with
    | ex -> 
      sprintf @"Entity type, %s, does not contain the attribute %s. %s. Entity Id: %s"
        (x.LogicalName) key (getFullException(ex)) (x.Id.ToString())
      |> failwith 

  let getCodeValue key (x:Entity) (log:ConsoleLogger) =
    getAttribute key x log :?> OptionSetValue
    |> fun e -> e.Value

  // Fetches the entity of views in a packaged solution file from an exported 
  // solution
  let getViews p (xmlFile:string) log = 

    // Find the xml files for the 
//    let xmlFiles = 
//      Directory.GetDirectories entitiesFolder
//      |> Array.map(fun path ->
//        Path.Combine(path, "SavedQueries/")
//        |> Directory.GetFiles
//        |> Array.toSeq)
//      |> Array.toSeq
//      |> Seq.concat
   
    // parse the customization.xml and find all the nodes containing "savedquery"
    // under the node "SavedQueries"
    let savedQueryGuids =
//      xmlFiles
//      |> Seq.map(fun (xmlFile:string) ->
        let doc = new XmlDocument()
        doc.Load xmlFile
        doc.SelectNodes "//savedqueryid/text()"
        |> Seq.cast<XmlNode> 
        |> Seq.map(fun node -> Guid.Parse node.Value)

    // fetch the entities of the views
    let savedQueries =
      savedQueryGuids
      |> Seq.map(fun guid -> CrmData.CRUD.retrieve p "savedquery" guid)

    // return a sequence of the found guids
    savedQueries

  // Stores entities statecode and statuscode in a seperate file to be
  // implemented on import
  let exportStates' org ac zipPath (log:ConsoleLogger) =

    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc

    // Retriev Customization.xml file from the solution package and store it in 
    // a temp folder
    let tempFolder = createTempFolder

    log.WriteLine(LogLevel.Verbose, @"Extracting file from solution")

    use zipToOpen = new FileStream(zipPath, FileMode.Open)
    use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)
    let xmlFile = Path.Combine(tempFolder, "customizations.xml")
    let entry = archive.GetEntry("customizations.xml")
    entry.ExtractToFile(xmlFile)

    log.WriteLine(LogLevel.Verbose, @"Finding entities 'statusCode' and 'stateCode'")
    // find the entities to be persisted
    let entities =
      [getViews p xmlFile log]
      |> List.toSeq
      |> Seq.concat

    // Add state and statuscodes to a delegateSolution record
    let states =
      entities
      |> Seq.filter(fun entity -> 
        entity.Attributes.ContainsKey "statuscode" && 
        entity.Attributes.ContainsKey "statecode")
      |> Seq.map(fun entity ->
        let guidState = 
          {id = entity.Id
           logicalName = entity.LogicalName
           stateCode = getCodeValue "statecode" entity log
           statusCode = getCodeValue "statuscode" entity log}
        (entity.Id.ToString(), guidState))
      |> Map.ofSeq

    let delegateSolution = {states=states}
    
    log.WriteLine(LogLevel.Verbose, @"Creating code file")

    // Serialize the record to an xml file called dgSolution.xml and add it to the
    // packaged solution 
    let arr = SerializationHelper.serializeXML<DelegateSolution> delegateSolution

    let stateEntry = archive.CreateEntry("dgSolution.xml")
    use writer = new StreamWriter(stateEntry.Open())
    writer.BaseStream.Write(arr,0,arr.Length)

    Directory.Delete(tempFolder,true)


  let importStates' org ac zipPath (log:ConsoleLogger)=

    // Check that the packaged solution contains a dgSolution.xml file
    use zipToOpen = new FileStream(zipPath, FileMode.Open)
    use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)

    match archive.Entries |> Seq.exists(fun e -> e.Name = "dgSolution.xml") with                                        
    | false -> 
      log.WriteLine(LogLevel.Verbose, @"No stored dgSolution.xml file found")

    | true -> 
      let m = ServiceManager.createOrgService org
      let tc = m.Authenticate(ac)
      use p = ServiceProxy.getOrganizationServiceProxy m tc
      // Fetch the dgSolution.xml file and unserialize it
      
      let entry = archive.GetEntry("dgSolution.xml")
      use writer = new StreamReader(entry.Open())

      let xmlContent = writer.ReadToEnd()
      
      let dgSol = SerializationHelper.deserializeXML<DelegateSolution> xmlContent

      // Read the status and statecode of the entities and update them in crm
      // Find the source entities that have different code values than target
      let diffdgSol =
        dgSol.states
        |> Map.toArray
        |> Array.map(fun (_,guidState) -> 
           CrmData.CRUD.retrieveReq guidState.logicalName guidState.id :> OrganizationRequest )
        |> DataHelper.performAsBulk p
        |> Array.map(fun resp -> 
          let resp' = resp.Response :?> Messages.RetrieveResponse 
          resp'.Entity)
        |> Array.filter(fun target -> 
          let source = dgSol.states.[target.Id.ToString()]
          getCodeValue "statecode" target log <> source.stateCode ||
          getCodeValue "statuscode" target log <> source.statusCode)
        |> Array.map(fun target ->  
          dgSol.states.[target.Id.ToString()])
        
      // update the entities states
      diffdgSol
      |> Array.map(fun x -> 
        CrmDataInternal.Entities.updateStateReq x.logicalName x.id x.stateCode x.statusCode :> OrganizationRequest )
      |> DataHelper.performAsBulk p
      |> ignore
        
    ()