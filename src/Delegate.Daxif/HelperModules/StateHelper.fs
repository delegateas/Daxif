namespace DG.Daxif.HelperModules

open System
open System.IO
open System.Xml
open System.Text
open Microsoft.Crm.Sdk
open Microsoft.Crm.Tools.SolutionPackager
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility
open DG.Daxif.HelperModules.Common.ConsoleLogger

module internal StateHelper =

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

   // extract a solution from a package
  let extractSP zip path (log:ConsoleLogger) =
    let logl = Enum.GetName(typeof<LogLevel>,log.LogLevel)
    
    let pa = new PackagerArguments()

    log.WriteLine(LogLevel.Info,"Start output from SolutionPackager")

    // Use parser to ensure correct initialization of arguments
    Parser.ParseArgumentsWithUsage(
        [|"/action:Extract"; 
        sprintf @"/zipfile:%s" zip ;
        sprintf @"/folder:%s" path; 
        sprintf @"/errorlevel:%s" logl;
        "/allowDelete:Yes";
        "/clobber"|],
        pa) |> ignore
        
    try 
      let sp = new SolutionPackager(pa)
      sp.Run()  
    with 
      ex -> log.WriteLine(LogLevel.Error,sprintf "%s" ex.Message )

  // Fetches the entity of views in a packaged solution file from an exported 
  // solution
  let getViews p tempFolder log = 

    // Find the xml files for the 
    let entitiesFolder = Path.Combine(tempFolder, "Entities/")
    let xmlFiles = 
      Directory.GetDirectories entitiesFolder
      |> Array.map(fun path ->
        Path.Combine(path, "SavedQueries/")
        |> Directory.GetFiles
        |> Array.toSeq)
      |> Array.toSeq
      |> Seq.concat
   
    // parse the customization.xml and find all the nodes containing "savedquery"
    // under the node "SavedQueries"
    let savedQueryGuids =
      xmlFiles
      |> Seq.map(fun (xmlFile:string) ->
        let doc = new XmlDocument()
        doc.Load xmlFile
        doc.SelectSingleNode "//savedqueryid/text()"
        |> fun node -> Guid.Parse node.Value)

    // fetch the entities of the views
    let savedQueries =
      savedQueryGuids
      |> Seq.map(fun guid -> CrmData.CRUD.retrieve p "savedquery" guid)

    // return a sequence of the found guids
    savedQueries

  // Stores entities statecode and statuscode in a seperate file to be
  // implemented on import
  let exportStates' org ac solution zipPath log=

    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc

    // unpack the packaged solution to a temp folder
    let tempFolder = createTempFolder
    extractSP zipPath tempFolder log

    // find the entities
    let entities =
      [getViews p tempFolder log]
      |> List.toSeq
      |> Seq.concat

    let entityCodes =
      entities
      |> Seq.filter(fun entity -> 
        entity.Attributes.ContainsKey "statuscode" && 
        entity.Attributes.ContainsKey "statecode")
      |> Seq.map(fun entity ->
        (entity.Id, entity.LogicalName, 
          (getCodeValue "statuscode" entity log, 
            getCodeValue "statecode" entity log)))

    // Add state and statuscodes to class store

    // Serialize the class to an xml file called dgSolution.xml

    // Add the new xml file to temp folder

    // Pack the tempfolder and add "-dgSolution" to the end

    // Delete the temp folder
    Directory.Delete(tempFolder,true)

    ()


  let importStates' org ac solution path log'=
    
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc

    let s = CrmDataInternal.Entities.retrieveSolution p solution

    // Unpack the the solution with "-dgSolution" to a temp position
    
    // Unserialize the dgSolution xml file

    // Read the status and statecode and update them in crm
    
    // Delete the temp folder
    
    ()