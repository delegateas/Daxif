namespace DG.Daxif.HelperModules

open System
open System.IO
open System.Reflection
open System.Xml.Linq
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility

(*
  This module is used to synchronize a plugin assembly to a solution in CRM. 
  The assemblies are build with an extended Plugin.cs see http://delegateas.github.io/Delegate.Daxif/plugin-reg-setup.html
  This script enables Daxif to fetch data of each plugin through incovation.
  Each plugin is validated in order to ensure that the plugins are correclty configured.
  If the plugin is valid then Daxif will syncronize the plugins in CRM.
*)

module internal PluginsHelper =

  type ExecutionMode = 
    | Synchronous = 0
    | Asynchonous = 1

  type ExecutionStage = 
    | PreValidation = 10
    | Pre = 20
    | Post = 40


  // Records for encapsulating step and images in a plugin
  type Step =
    { className: String
      executionStage: int
      eventOperation: String
      logicalName: String
      deployment: int
      executionMode: int
      name: String
      executionOrder: int
      filteredAttributes: String
      userContext: Guid } with
    member this.messageName = 
      let entity' = String.IsNullOrEmpty(this.logicalName) |> function
          | true -> "any Entity" | false -> this.logicalName
      let execMode = (enum<ExecutionMode> this.executionMode).ToString()
      let execStage = (enum<ExecutionStage> this.executionStage).ToString()
      sprintf "%s: %s %s %s of %s" this.className execMode execStage this.eventOperation entity'

  type Image = 
    { name: string
      entityAlias: string
      imageType: int
      attributes: string }

  type Plugin =
    { step: Step
      images: seq<Image> } with
    member this.TypeKey = 
      this.step.className
    member this.StepKey = 
      this.step.messageName
    member this.ImagesWithKeys =
      this.images
      |> Seq.map(fun image -> sprintf "%s, %s" this.StepKey image.name, image)

  // Record holding variouse information regarding the Solution and connecting
  // to a CRM instance.
  type Solution =
    { assembly: Assembly
      assemblyId: Option<Guid>
      dllName: String
      dllPath: String
      hash: String
      entity: Entity 
      isolationMode: PluginIsolationMode}

  type ClientManagement = 
    { IServiceM: Client.IServiceManagement<IOrganizationService>
      authCred: Client.AuthenticationCredentials }

  /// Helpers functions
  // Fetches the name of an entity
  let getName (x:Entity) = x.Attributes.["name"] :?> string

  // Tries to get an attribute from an entit. Fails if the attribute does not exist.
  let getAttribute key (x:Entity) = 
    try
      x.Attributes.[key]
    with
    | ex -> 
      sprintf @"Entity type, %s, does not contain the attribute %s. %s"
        (x.LogicalName) key (getFullException(ex))
      |> failwith 

  let isDefaultGuid guid =
    guid = Guid.Empty

  // Used to create a temprorary organization proxy to connect to CRM
  let proxyContext' client f =
    ServiceProxy.proxyContext client.IServiceM client.authCred f

  let subsetEntity ys (xs:string Set)  = 
    ys
    |> Seq.filter(fun (y,_) -> Seq.exists (fun x -> x = y) xs)
    |> Seq.map snd

  let setfstSeq seq =
      seq
      |> Seq.map fst
      |> Set.ofSeq

  // Tries to fetch an attribute. If the attribute does not exist,
  // a default value is returned.
  let defaultAttributeVal (e:Entity) key (def:'a) =
    match e.Attributes.TryGetValue(key) with
    | (true,v) -> v :?> 'a
    | (false,_) -> def

  (* 
    Module used to validate that each step and images are correctly configured.
    If an invalid step or image is found and error is produced an no further 
    test are performed
  *)
  module Validation =
     
     // Types for steps and image parameters
    type ExecutionMode =
      | Synchronous = 0
      | Asynchronous = 1

    type ExecutionStage =
      | PreValidation = 10
      | PreOperation = 20
      | PostOperation = 40

    type ImageType =
      | PreImage = 0
      | PostImage = 1
      | Both = 2

    // Helper functions and monads for step based testing
    type Result<'TValid,'TInvalid> = 
      | Valid of 'TValid
      | Invalid of 'TInvalid

    let bind switchFunction = function
      | Valid s -> switchFunction s
      | Invalid f -> Invalid f
           
    let findInvalid  plugins invalidPlugins msg =
      match invalidPlugins |> Seq.tryPick Some with
        | Some(name,_) -> Invalid (sprintf msg name)
        | None -> Valid plugins

    // Functions testing different aspects of the plugins
    let preOperationNoPreImages plugins =
      let invalids =
        plugins
        |> Seq.filter(fun (_,pl) ->
          let i' = 
            pl.images
            |> Seq.filter(fun image -> image.imageType = int ImageType.PostImage)
          (pl.step.executionStage = int ExecutionStage.PreOperation || 
            pl.step.executionStage = int ExecutionStage.PreValidation) &&
              not (Seq.isEmpty i'))

      findInvalid plugins invalids "Plugin %s: Pre execution stages does not support pre-images"

    let postOperationNoAsync plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,pl) -> 
          pl.step.executionMode = int ExecutionMode.Asynchronous && 
            pl.step.executionStage <> int ExecutionStage.PostOperation)

      findInvalid plugins invalidPlugins "Plugin %s: Post execution stages does not support asynchronous execution mode"

    let associateDisasociateSteps plugins =
      plugins
      |> Seq.filter(fun (_,pl) -> 
        pl.step.eventOperation = "Associate" ||
        pl.step.eventOperation = "Disassociate")
        
    let associateDisassociateNoFilters plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,pl) ->
          pl.step.filteredAttributes <> null)
            
      findInvalid plugins invalidPlugins "Plugin %s can't have filtered attributes"

    let associateDisassociateNoImages plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,pl) ->
          not (Seq.isEmpty pl.images))
            
      findInvalid plugins invalidPlugins "Plugin %s can't have images"

    let associateDisassociateAllEntity plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,pl) ->
          pl.step.logicalName <> "")
            
      findInvalid plugins invalidPlugins "Plugin %s must target all entities"

    let preEventsNoPreImages plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,pl) ->
          let i' = 
            pl.images
            |> Seq.filter(fun image -> image.imageType = int ImageType.PreImage)
          pl.step.eventOperation = "Create" &&
          not (Seq.isEmpty i'))
            
      findInvalid plugins invalidPlugins "Plugin %s: Create events does not support pre-images"

    let postEventsNoPostImages plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,pl) ->
          let i' = 
            pl.images
            |> Seq.filter(fun image -> image.imageType = int ImageType.PostImage)
          pl.step.eventOperation = "Delete" &&
          not (Seq.isEmpty i'))
            
      findInvalid plugins invalidPlugins "Plugin %s: Post-events does not support post-images"

    let validUserContext client plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,pl) -> isDefaultGuid pl.step.userContext |> not)
        |> Seq.filter(fun (_,pl) ->
          proxyContext' client (fun p ->
            try 
              match CrmData.CRUD.retrieve p "systemuser" pl.step.userContext with
              | _ -> false
            with _ -> true 
          )
        )

      findInvalid plugins invalidPlugins "Plugin %s: Defined user context is not in the system"
    
    // Collection of all validation steps

    let validateAssociateDisassosiate =
      associateDisassociateNoFilters
      >> bind associateDisassociateNoImages
      >> bind associateDisassociateAllEntity

    let validate client =
      postOperationNoAsync
      >> bind preOperationNoPreImages
      >> bind validateAssociateDisassosiate
      >> bind preEventsNoPreImages
      >> bind postEventsNoPostImages
      >> bind (validUserContext client)

    let validatePlugins plugins client =
      plugins
      |> Seq.map(fun pl -> (pl.step.messageName,pl))
      |> validate client

  // Used to set the requied attribute messagePropertyName 
  // based on the message class when creating images
  let propertyName =
    Map.empty
      .Add("Assign","Target")
      .Add("Create","id")
      .Add("Delete","Target")
      .Add("DeliverIncoming","emailid")
      .Add("DeliverPromote","emailid")
      .Add("Merge","Target")
      //.Add("Merge","Subordinatedid") // undeterministic
      .Add("Route","Target")
      .Add("Send","emailid")
      .Add("SetState","entityMoniker")
      .Add("SetStateDynamicEntity","entityMoniker")
      .Add("Update","Target")
    
  // Used to retrieve a .vsproj dependencies (recursive)
  let projDependencies (vsproj:string) = 
    let getElemName name =
      XName.Get(name, "http://schemas.microsoft.com/developer/msbuild/2003")
      
    let getElemValue name (parent : XElement) = 
      let elem = parent.Element(getElemName name)
      if elem = null || String.IsNullOrEmpty elem.Value then None
      else Some(elem.Value)
      
    let getAttrValue name (elem : XElement) = 
      let attr = elem.Attribute(XName.Get name)
      if attr = null || String.IsNullOrEmpty attr.Value then None
      else Some(attr.Value)
      
    let (|??) (option1 : 'a Option) option2 = 
      if option1.IsSome then option1
      else option2

    let fullpath path1 path2 = Path.GetFullPath(Path.Combine(path1, path2))

    let rec projDependencies' vsproj' = seq {
      let vsProjXml = XDocument.Load(uri = vsproj')

      let path = Path.GetDirectoryName(vsproj')

      let projRefs = 
        vsProjXml.Document.Descendants(getElemName "ProjectReference")
        |> Seq.choose (fun elem -> getAttrValue "Include" elem)
        |> Seq.map(fun elem -> fullpath path elem)

      let refs = 
        vsProjXml.Document.Descendants(getElemName "Reference")
        |> Seq.choose (fun elem -> getElemValue "HintPath" elem |?? getAttrValue "Include" elem)
        |> Seq.filter (fun ref -> ref.EndsWith(".dll"))
        |> Seq.map(fun elem -> fullpath path elem)
      
      let files = 
        vsProjXml.Document.Descendants(getElemName "Compile")
        |> Seq.choose (fun elem -> getAttrValue "Include" elem)
        |> Seq.map(fun elem -> fullpath path elem)
      
      for projRef in projRefs do
        yield! projDependencies' projRef
      yield! refs
      yield! files }

    projDependencies' vsproj

  // Transforms the received tuple from the assembly file through invocation into
  // plugin, step and image records
  let tupleToRecord ((a,b,c,d),(e,f,g,h,i,j),images) = 
    let step = 
      { className = a; executionStage = b; eventOperation = c;
        logicalName = d; deployment = e; executionMode = f;
        name = g; executionOrder = h; filteredAttributes = i; 
        userContext = Guid.Parse(j)}
    let images' =
      images
      |> Seq.map( fun (j,k,l,m) ->
        { name = j; entityAlias = k;
          imageType = l; attributes = m; } )
    { step = step; images = images' }  

  // Calls "PluginProcessingStepConfigs" in the plugin assembly that returns a
  // tuple contaning the plugin informations
  let typesAndMessages (asm:Assembly) =
    try
      asm.GetTypes() |> fun xs -> 
        let y = xs |> Array.filter (fun x -> x.Name = @"Plugin") |> Array.toList
                    |> List.head
        xs
        |> Array.filter (fun (x:Type) -> x.IsSubclassOf(y))
        |> Array.Parallel.map (fun (x:Type) -> 
          Activator.CreateInstance(x), x.GetMethod(@"PluginProcessingStepConfigs"))
        |> Array.Parallel.map (fun (x, (y:MethodInfo)) -> 
            y.Invoke(x, [||]) :?> 
              ((string * int * string * string) * 
                (int * int * string * int * string * string) * 
                  seq<(string * string * int * string)>) seq)
        |> Array.toSeq
        |> Seq.concat
        |> Seq.map( fun x -> tupleToRecord x )
    with
    | ex -> 
      sprintf @"Failed to fetch plugin configuration from plugin assembly. This error can be caused if an old version of Plugin.cs is used. Full Exception: %s"
        (getFullException(ex))
      |> failwith 
      

  // Creates a new assembly in CRM with the provided information
  let createAssembly name dll (asm:Assembly) hash (isolationMode:PluginIsolationMode) =
    let pa = Entity("pluginassembly")
    pa.Attributes.Add("name", name)
    pa.Attributes.Add("content", dll |> fileToBase64)
    pa.Attributes.Add("sourcehash", hash)
    pa.Attributes.Add("isolationmode", OptionSetValue(int isolationMode)) // sandbox OptionSetValue(2)
    pa.Attributes.Add("version", asm.GetName().Version.ToString())
    pa.Attributes.Add("description", syncDescription())
    pa

  // Updates an existing assembly in CRM with the provided assembly information
  let updateAssembly (paid:Guid) dll (asm:Assembly) hash (isolationMode:PluginIsolationMode) =
    let pa = Entity("pluginassembly")
    pa.Attributes.Add("pluginassemblyid", paid)
    pa.Attributes.Add("content", dll |> fileToBase64)
    pa.Attributes.Add("sourcehash", hash)
    pa.Attributes.Add("isolationmode", OptionSetValue(int isolationMode))
    pa.Attributes.Add("version", asm.GetName().Version.ToString())
    pa.Attributes.Add("description", syncDescription())
    pa

  // Create a new type in CRM under the defined assembly id
  let createType (asmId:Guid) (name:string) =
    let pt = Entity("plugintype")
    pt.Attributes.Add("name", name)
    pt.Attributes.Add("typename", name)
    pt.Attributes.Add("friendlyname", Guid.NewGuid().ToString())
    pt.Attributes.Add("pluginassemblyid", EntityReference("pluginassembly",asmId))
    pt.Attributes.Add("description", syncDescription())
    pt

  // Create a new step with the provided step information in CRM under the defined type
  let createStep (typeId:Guid) (messageId:Guid) (filterId:Guid) name step =
    let ps = Entity("sdkmessageprocessingstep")
    ps.Attributes.Add("name", name)
    ps.Attributes.Add("asyncautodelete", false)
    ps.Attributes.Add("rank", step.executionOrder)
    ps.Attributes.Add("mode", OptionSetValue(step.executionMode))
    ps.Attributes.Add("plugintypeid", EntityReference("plugintype",typeId))
    ps.Attributes.Add("sdkmessageid", EntityReference("sdkmessage",messageId))
    ps.Attributes.Add("stage", OptionSetValue(step.executionStage))
    ps.Attributes.Add("filteringattributes", step.filteredAttributes)
    ps.Attributes.Add("supporteddeployment", OptionSetValue(step.deployment))
    ps.Attributes.Add("description", syncDescription())
    match isDefaultGuid step.userContext with
     | true -> ()
     | false -> ps.Attributes.Add("impersonatinguserid", EntityReference("systemuser",step.userContext))
    String.IsNullOrEmpty(step.logicalName) |> function
      | true  -> ()
      | false ->
        ps.Attributes.Add("sdkmessagefilterid",
          EntityReference("sdkmessagefilter",filterId))
    ps

  // Create a new image with the provided image informations under the defined step
  let createImage (stepId:Guid) stepName image =
    let psi = Entity("sdkmessageprocessingstepimage")
    psi.Attributes.Add("name", image.name)
    psi.Attributes.Add("entityalias", image.entityAlias)
    psi.Attributes.Add("imagetype", OptionSetValue(image.imageType))
    psi.Attributes.Add("attributes", image.attributes)
    psi.Attributes.Add("messagepropertyname", propertyName.[stepName])
    psi.Attributes.Add("sdkmessageprocessingstepid", 
      EntityReference("sdkmessageprocessingstep",stepId))
    psi

  // Used to update an existing step with changes to its attributes
  // Only check for updated on stage, deployment, mode, rank and filteredAttributes. 
  // The rest must be update by UI
  let updateStep (pmid:Guid) step =
    let ps = Entity("sdkmessageprocessingstep")
    ps.Attributes.Add("sdkmessageprocessingstepid", pmid)
    ps.Attributes.Add("stage", OptionSetValue(step.executionStage))
    ps.Attributes.Add("filteringattributes", step.filteredAttributes)
    ps.Attributes.Add("supporteddeployment", OptionSetValue(step.deployment))
    ps.Attributes.Add("mode", OptionSetValue(step.executionMode))
    ps.Attributes.Add("rank", step.executionOrder)
    ps.Attributes.Add("description", syncDescription())
    match isDefaultGuid step.userContext with
     | true -> ps.Attributes.Add("impersonatinguserid", null)
     | false -> ps.Attributes.Add("impersonatinguserid", EntityReference("systemuser",step.userContext))
    ps

  // Used to update an existing image with changes to its attributes
  let updateImage (pmid:Guid) image = 
    let psi = Entity("sdkmessageprocessingstepimage")
    psi.Attributes.Add("sdkmessageprocessingstepimageid", pmid)
    psi.Attributes.Add("name", image.name)
    psi.Attributes.Add("entityalias", image.entityAlias)
    psi.Attributes.Add("imagetype", OptionSetValue(image.imageType))
    psi.Attributes.Add("attributes", image.attributes)
    psi

  let instantiateAssembly (solution:Solution) p isolationMode (log:ConsoleLogger.ConsoleLogger) =
    log.WriteLine(LogLevel.Verbose, "Creating Assembly")
    let pa = createAssembly solution.dllName solution.dllPath solution.assembly solution.hash isolationMode
    let pc = ParameterCollection()

    pc.Add("SolutionUniqueName", getAttribute "uniquename" solution.entity)

    let guid = CrmData.CRUD.create p pa pc

    log.WriteLine(LogLevel.Verbose,
        sprintf "%s: (%O) was created" pa.LogicalName guid)
    guid

  // Attempts to fetch and existing Assembly in CRM
  let fetchAssemblyId (solution:Entity) dllName dllPath asm hash p 
    (log:ConsoleLogger.ConsoleLogger) =
      log.WriteLine(LogLevel.Verbose, "Retrieving assemblies from CRM")
      CrmDataInternal.Entities.retrievePluginAssemblies p solution.Id
      |> Seq.filter(fun x -> dllName = getName x)
      |> Seq.map(fun x -> x.Id)
      |> fun x -> if Seq.isEmpty x then None else x |> Seq.head |> Some

  // Fetches data from the dll file and the plugins from the provided CRM solution.
  // Returns a tuple contaning a ClientManagement and Solution record along with a 
  // sequence of the plugins
  let setupData org ac solutionName proj dll isolationMode (log:ConsoleLogger.ConsoleLogger) =
    log.WriteLine(LogLevel.Verbose, "Authenticating credentials")
    
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc
    let client = { IServiceM = m; authCred = tc}

    log.WriteLine(LogLevel.Verbose, "Authentication completed")

    log.WriteLine(LogLevel.Verbose, "Checking local assembly")
    let dll'  = Path.GetFullPath(dll)
    let tmp   = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString() + @".dll")

    File.Copy(dll',tmp,true)

    let dllPath = Path.GetFullPath(tmp)
    let proj' = Path.GetFullPath(proj)
    let hash =
        projDependencies proj' |> Set.ofSeq
        |> Set.map(fun x -> File.ReadAllBytes(x) |> sha1CheckSum')
        |> Set.fold(fun a x -> a + x |> sha1CheckSum) String.Empty

    let asm = Assembly.LoadFile(dllPath); 
    let dllName = Path.GetFileNameWithoutExtension(dll'); 
  
    let solutionEntity = CrmDataInternal.Entities.retrieveSolution p solutionName

    let sourcePlugins = typesAndMessages asm

    log.WriteLine(LogLevel.Verbose, "Validating plugins to be registered")
    match Validation.validatePlugins sourcePlugins client with
      | Validation.Invalid x ->
        failwith x
      | Validation.Valid _ -> 
        log.WriteLine(LogLevel.Verbose, "Validation completed")       

        let asmId = fetchAssemblyId solutionEntity dllName dllPath asm hash p log
        let solution =
            { assembly = asm;
              assemblyId = asmId;
              dllName = dllName;
              dllPath = dllPath
              hash = hash;
              entity = solutionEntity;
              isolationMode = isolationMode}
        client, solution, sourcePlugins

  // Fill plugin map with steps with the execution stage and step name as primary key
  let getPluginTypes asmId client (log:ConsoleLogger.ConsoleLogger) pluginContent =

    proxyContext' client (fun p -> 
      let types = CrmDataInternal.Entities.retrievePluginTypes p asmId
      log.WriteLine(LogLevel.Debug, sprintf "Retrieved %d types" (Seq.length types))

      types
      |> Seq.map(fun t -> getName t, t)
      |> Map.ofSeq
      |> fun x -> Map.add "Types" x pluginContent
    )

  // Fill plugin map with steps with the execution stage and step name as primary key
  let getPluginSteps solutionId client (log:ConsoleLogger.ConsoleLogger) pluginContent =

    proxyContext' client (fun p -> 
      let steps = CrmDataInternal.Entities.retrieveAllPluginProcessingSteps p solutionId
      log.WriteLine(LogLevel.Debug, sprintf "Found %d steps" (Seq.length steps))

      steps
      |> Seq.map(fun step -> getName step, step)
      |> Map.ofSeq
      |> fun x -> Map.add "Steps" x pluginContent
    )

  // Fill plugin map with steps with the execution stage and step name as primary key
  let getPluginImages client (log:ConsoleLogger.ConsoleLogger) (pluginContent: Map<string,Map<string,Entity>>) =
    pluginContent.["Steps"]
    |> Map.toArray
    |> Array.Parallel.map(fun (stepKey, step) ->
      proxyContext' client (fun p' -> 
        let stepName = getName step 
        let images = CrmDataInternal.Entities.retrievePluginProcessingStepImages p' step.Id

        if images |> Seq.isEmpty |> not
        then log.WriteLine(LogLevel.Debug, sprintf "Found %d images in step %s" (Seq.length images) stepName)

        images
        |> Seq.map(fun image -> sprintf "%s, %s" stepKey (getName image), image)
        |> Seq.toArray
      )
    )
    |> Array.concat
    |> Map.ofSeq
    |> fun x -> Map.add "Images" x pluginContent

  let deleteEntities (log:ConsoleLogger.ConsoleLogger) client (images: seq<Entity>) = 
    images
    |> Seq.toArray
    |> Array.Parallel.map(fun y -> 
      proxyContext' client (fun p -> 
        y, CrmData.CRUD.delete p y.LogicalName y.Id) )
    |> Array.iter(
      fun (x,_) -> 
      log.WriteLine(LogLevel.Verbose,
        sprintf "%s: %s was deleted (%O)" x.LogicalName (getName x) x.Id))

  let deleteObsoleteEntities client (log:ConsoleLogger.ConsoleLogger) target source =
    let tSet = setfstSeq target
    let sSet = setfstSeq source
    let obsoleteSet = tSet - sSet
    obsoleteSet
    |> subsetEntity target
    |> deleteEntities log client

  let deleteImages client (log:ConsoleLogger.ConsoleLogger) (sourcePlugins:seq<Plugin>) (pluginContent: Map<string,Map<string,Entity>>) =
    log.WriteLine(LogLevel.Info, "Deleting images")
    let sourceImages =
      sourcePlugins
      |> Seq.map(fun pl -> pl.ImagesWithKeys)
      |> Seq.concat
    let targetImages = 
        pluginContent.["Images"]
        |> Map.toSeq
    deleteObsoleteEntities client log targetImages sourceImages

  let deleteSteps client (log:ConsoleLogger.ConsoleLogger) (sourcePlugins:seq<Plugin>) (pluginContent: Map<string,Map<string,Entity>>) =
    log.WriteLine(LogLevel.Info, "Deleting steps")
    let sourceSteps =
      sourcePlugins
      |> Seq.map(fun pl -> pl.StepKey, pl)
    let targetSteps = 
        pluginContent.["Steps"]
        |> Map.toSeq
    deleteObsoleteEntities client log targetSteps sourceSteps

  let deleteTypes client (log:ConsoleLogger.ConsoleLogger) (sourcePlugins:seq<Plugin>) (pluginContent: Map<string,Map<string,Entity>>) =
    log.WriteLine(LogLevel.Info, "Deleting types")
    let sourceTypes =
      sourcePlugins
      |> Seq.map(fun pl -> pl.TypeKey, pl)
    let targetTypes = 
        pluginContent.["Types"]
        |> Map.toSeq

    deleteObsoleteEntities client log targetTypes sourceTypes

  // Update if there is no sourcehash, sourcehash is different, isolationmode is different
  let updateAssembly' solution client (log:ConsoleLogger.ConsoleLogger) =
    log.WriteLine(LogLevel.Verbose, "Retrieving assemblies from CRM")

    proxyContext' client (fun p -> 

      CrmDataInternal.Entities.retrievePluginAssemblies p solution.entity.Id
      |> Seq.filter(fun x -> solution.dllName = getName x)
      |> Seq.iter(fun x ->
        let changes = 
          x.Attributes.ContainsKey "sourcehash" |> not
          || solution.hash <> (getAttribute "sourcehash" x :?> string)
          || (int solution.isolationMode) <> ((getAttribute "isolationmode" x :?> OptionSetValue).Value)
        match changes with
        | false -> ()
        | true -> 
          log.WriteLine(LogLevel.Info, "Updating Assembly")
          CrmData.CRUD.update p (updateAssembly x.Id solution.dllPath solution.assembly solution.hash solution.isolationMode) 
          |> ignore

          log.WriteLine(LogLevel.Verbose, sprintf "%s: %s was updated" x.LogicalName (getName x)) ) )

  let createTypes (log:ConsoleLogger.ConsoleLogger) client entitySet assemblyId =
    entitySet
    |> Seq.toArray
    |> Array.Parallel.iter(fun x ->
      proxyContext' client (fun p -> 
        let pt = createType assemblyId x
        CrmData.CRUD.create p pt (ParameterCollection())|> ignore
        log.WriteLine(LogLevel.Verbose, 
          sprintf "%s: %s was created" pt.LogicalName (getName pt))) )

  let insertTypes (solution: Solution) client (log:ConsoleLogger.ConsoleLogger) (sourcePlugins:seq<Plugin>) (pluginContent: Map<string,Map<string,Entity>>) =
    log.WriteLine(LogLevel.Info, sprintf "Creating types")

    let sourceImageSet =
      sourcePlugins
      |> Seq.map(fun pl -> pl.TypeKey, pl)
      |> setfstSeq

    let targetImageSet = 
        pluginContent.["Types"]
        |> Map.toSeq
        |> setfstSeq

    sourceImageSet - targetImageSet
    |> fun x -> createTypes log client x solution.assemblyId.Value

  let createSteps (log:ConsoleLogger.ConsoleLogger) (solution:Entity) 
    client steps (plugins:Plugin seq) (pluginType: Entity) = 
      steps 
      |> Seq.toArray
      |> Array.Parallel.map(fun y ->
        proxyContext' client (fun p -> 
          let step = 
            plugins
            |> Seq.filter(fun pl -> y = pl.StepKey)
            |> Seq.head
            |> (fun pl -> pl.step)

          let sdkm = CrmDataInternal.Entities.retrieveSdkMessage p step.eventOperation
          let sdkf =
            CrmDataInternal.Entities.retrieveSdkMessageFilter
              p step.logicalName sdkm.Id

          let ps =
            createStep pluginType.Id sdkm.Id sdkf.Id 
              step.messageName step
          let pc = ParameterCollection()
          pc.Add("SolutionUniqueName", getAttribute "uniquename" solution)

          ps, CrmData.CRUD.create p ps pc) )
      |> Array.iter(
        fun (x,_) -> 
          log.WriteLine(LogLevel.Verbose,
            sprintf "%s: (%O) was created" x.LogicalName (getName x)))

  let updateSteps (log:ConsoleLogger.ConsoleLogger) client entity plugins =
    entity
    |> Seq.toArray
    |> Array.Parallel.map(fun y ->
      proxyContext' client (fun p -> 
        let name = getName y
        let stage = 
          defaultAttributeVal y "stage" (OptionSetValue(20))
        let deploy = 
          defaultAttributeVal y "supporteddeployment" (OptionSetValue(0))
        let mode = 
          defaultAttributeVal y "mode" (OptionSetValue(0))
        let order = 
          defaultAttributeVal y "rank" 0
        let filteredA = 
          defaultAttributeVal y "filteringattributes" null
        let userContext = 
          defaultAttributeVal y "impersonatinguserid" null
          |> function
            | null -> Guid.Empty
            | (x: EntityReference) -> x.Id

        let step, update =
          plugins
          |> Seq.filter(fun pl -> name = pl.step.messageName)
          |> Seq.head
          |> fun pl -> 
            (pl.step,(pl.step.executionStage,pl.step.deployment,pl.step.executionMode,
              pl.step.executionOrder,pl.step.filteredAttributes,pl.step.userContext))

        match (stage.Value,deploy.Value,mode.Value,order,filteredA,userContext) = update with
        | true ->  None
        | false ->
          let stepEntity = updateStep y.Id step
          CrmData.CRUD.update p stepEntity |> ignore
          Some y ) )
    |> Array.choose(id)
    |> Array.iter(
      fun x -> 
        log.WriteLine(LogLevel.Verbose,
          sprintf "%s: %s was updated" x.LogicalName (getName x)))

  let upsertSteps (solution: Solution) client (log:ConsoleLogger.ConsoleLogger) (sourcePlugins:seq<Plugin>) (pluginContent: Map<string,Map<string,Entity>>) =
    log.WriteLine(LogLevel.Info, sprintf "Creating and updating steps")

    sourcePlugins
    |> Seq.groupBy(fun pl -> pl.step.className)
    |> Seq.toArray
    |> Array.Parallel.iter(fun (key,values) -> 
      proxyContext' client (fun p -> 

        let pt = 
          match pluginContent.["Types"].ContainsKey(key) with
          | true -> pluginContent.["Types"].[key]
          | false -> CrmDataInternal.Entities.retrievePluginType p key

        let sourceStepSet =
          values
          |> Seq.map(fun pl -> pl.StepKey, pl)
          |> setfstSeq

        let targetSteps = 
          pluginContent.["Steps"]
          |> Map.filter(fun x _ -> x.Contains(key))
          |> Map.toSeq

        let targetStepSet = setfstSeq targetSteps

        let newSteps = 
          sourceStepSet - targetStepSet
        let modfiedSteps = 
          Set.intersect sourceStepSet targetStepSet
          |> subsetEntity targetSteps
                
        if newSteps.IsEmpty |> not
        then 
          log.WriteLine(LogLevel.Debug, sprintf "Creating steps for: %s" key)
          createSteps log solution.entity client newSteps values pt

        if modfiedSteps |> Seq.isEmpty |> not
        then 
          log.WriteLine(LogLevel.Debug, sprintf "Updating steps for: %s" key)
          updateSteps log client modfiedSteps values) )

  let createImages (log:ConsoleLogger.ConsoleLogger) (solution:Entity) 
    client images (plugins:Plugin seq) (pluginStep: Entity) = 
      images
      |> Seq.toArray
      |> Array.Parallel.iter(fun y ->
        proxyContext' client (fun p -> 
          plugins
          |> Seq.map(fun pl -> 
            pl.ImagesWithKeys 
            |> Seq.map(fun image -> (pl.step,image)))
          |> Seq.concat
          |> Seq.filter(fun (_,(imageKey,_)) -> y = imageKey)
          |> Seq.toArray
          |> Array.map(fun (step,(_,image)) -> 
              let pm = createImage pluginStep.Id step.eventOperation image
              let pc = ParameterCollection()
              pc.Add("SolutionUniqueName", getAttribute "uniquename" solution)

              pm, CrmData.CRUD.create p pm pc) )
        |> Array.iter(
          fun (x,id) -> 
            log.WriteLine(LogLevel.Verbose,
              sprintf "%s: (%O) was created" x.LogicalName id)))

  let updateImages (log:ConsoleLogger.ConsoleLogger) client entity plugins =
    entity
    |> Seq.toArray
    |> Array.Parallel.map(fun y ->

      proxyContext' client (fun p -> 
        let compareImage =
          let name = getName y
          let alias = getAttribute "entityalias" y :?> string
          let imageType = 
            defaultAttributeVal y "imagetype" (OptionSetValue(0))
          let attributes = defaultAttributeVal y "attributes" null
          { name = name; entityAlias = alias; 
            imageType = imageType.Value; 
            attributes = attributes 
          }

        let updates =
          plugins
          |> Seq.map(fun pl -> pl.images)
          |> Seq.fold(fun acc i' -> 
            i' 
            |> Seq.append acc) Seq.empty
          |> Seq.filter(fun image -> compareImage.name = image.name)
          |> Seq.head

        match compareImage = updates with
        | true ->  None
        | false ->
          let psi = updateImage y.Id updates
          CrmData.CRUD.update p psi |> ignore
          Some y) )

    |> Array.choose(id)
    |> Array.iter(
      fun x -> 
        log.WriteLine(LogLevel.Verbose,
          sprintf "%s: (%O) was updated" x.LogicalName x.Id)) 

  let upsertImages (solution: Solution) client (log:ConsoleLogger.ConsoleLogger) (sourcePlugins:seq<Plugin>) (pluginContent: Map<string,Map<string,Entity>>) =
    log.WriteLine(LogLevel.Info, sprintf "Creating and updating images")

    sourcePlugins
    |> Seq.groupBy(fun pl -> (pl.step.messageName))
    |> Seq.toArray
    |> Array.Parallel.iter(fun (key,values) -> 
      proxyContext' client (fun p -> 

        let ps = 
          match pluginContent.["Steps"].ContainsKey(key) with
          | true -> pluginContent.["Steps"].[key]
          | false -> CrmDataInternal.Entities.retrieveSdkProcessingStep p key
        let sourceImageSet =
          values
          |> Seq.map(fun pl -> pl.ImagesWithKeys)
          |> Seq.concat
          |> setfstSeq

        let targetImages = 
          pluginContent.["Images"]
          |> Map.filter(fun x _ -> x.Contains(key))
          |> Map.toSeq

        let targetImageSet = setfstSeq targetImages
        let newImages = 
          sourceImageSet - targetImageSet
        let modifiedImages = 
          Set.intersect sourceImageSet targetImageSet
          |> subsetEntity targetImages
        
        if newImages.IsEmpty |> not
        then
          log.WriteLine(LogLevel.Debug, sprintf "Creating Images for: %s" key)
          createImages log solution.entity client newImages values ps

        if modifiedImages |> Seq.isEmpty |> not
        then
          log.WriteLine(LogLevel.Debug, sprintf "Updating Images for: %s" key)
          updateImages log client modifiedImages values) )

  // Deletes plugins that exist in target but not in source
  let deletePlugins org ac solutionName proj dll (log:ConsoleLogger.ConsoleLogger) =
    let (client, solution, sourcePlugins) = setupData org ac solutionName proj dll PluginIsolationMode.Sandbox log
    match solution.assemblyId with
    | Some assemblyId -> 
      log.WriteLine(LogLevel.Verbose, "Deleting plugins")
      // fetchData
      log.WriteLine(LogLevel.Verbose,"fetching plugin data from CRM")
      let targetData =
        Map.empty
        |> getPluginTypes assemblyId client log
        |> getPluginSteps solution.entity.Id client log
        |> getPluginImages client log

      // Delete old data
      deleteImages client log sourcePlugins targetData
      deleteSteps client log sourcePlugins targetData
      deleteTypes client log sourcePlugins targetData

    | None ->
      failwith "No plugin assembly found in solution"

  let syncPlugins (solution:Solution) client (log:ConsoleLogger.ConsoleLogger) sourceData =
    // fetchData
    log.WriteLine(LogLevel.Verbose,"fetching plugin data from CRM")
    let targetData =
      Map.empty
      |> getPluginTypes solution.assemblyId.Value client log
      |> getPluginSteps solution.entity.Id client log
      |> getPluginImages client log

    // Delete old data
    deleteImages client log sourceData targetData
    deleteSteps client log sourceData targetData
    deleteTypes client log sourceData targetData

    // update assembly
    updateAssembly' solution client log

    // upsert new data
    insertTypes solution client log sourceData targetData
    upsertSteps solution client log sourceData targetData
    upsertImages solution client log sourceData targetData


  // Syncronizes a solution
  let syncSolution org ac solutionName proj dll isolationMode (log:ConsoleLogger.ConsoleLogger) =
    let (client, solution, sourcePlugins) = setupData org ac solutionName proj dll isolationMode log
    let solution' =
      match solution.assemblyId with
      | Some _ -> 
        solution
      | None ->
        proxyContext' client (fun p -> 
          let asmId = instantiateAssembly solution p isolationMode log
          { assembly = solution.assembly;
            assemblyId = Some(asmId);
            dllName = solution.dllName;
            dllPath = solution.dllPath
            hash = solution.hash;
            entity = solution.entity;
            isolationMode = isolationMode})

    log.WriteLine(LogLevel.Verbose, "Syncing plugins")
    syncPlugins solution' client log sourcePlugins

  let syncSolution' org ac solutionName proj dll (log:ConsoleLogger.ConsoleLogger) =
    syncSolution org ac solutionName proj dll PluginIsolationMode.Sandbox log