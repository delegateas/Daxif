namespace DG.Daxif.HelperModules

open System
open System.IO
open System.Reflection
open System.Xml.Linq
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility

module internal PluginsHelper =


  type Step =
    { className: String;
      executionStage: int;
      eventOperation: String;
      logicalName: String;
      deployment: int;
      executionMode: int;
      name: String;
      executionOrder: int;
      filteredAttributes: String;
    }

  type Image = 
    {
      name: string;
      entityAlias: string;
      imageType: int; 
      attributes: string;
    }

  type Plugin =
    {
      step: Step;
      images: seq<Image>;
    }  
    

  // Helpers functions
  let getName (x:Entity) = x.Attributes.["name"] :?> string

  let getAttribute key (x:Entity) = 
    try
      x.Attributes.[key]
    with
    | ex -> 
      sprintf @"Entity type, %s, does not contain the attribute %s. %s"
        (x.LogicalName) key (getFullException(ex))
      |> failwith 

  let subset a (b:Entity seq) = 
    b |> Seq.filter(fun x -> 
      let z = getName x
      ((fun y -> y = z),a) ||> Seq.exists)
  let subset' (xs:(int*string) Set) (ys:(int*Entity) seq) = 
    ys
    |> Seq.filter(fun (y,y') -> 
      let z = getName y'
      ((fun x -> x = (y,z)),xs) ||> Seq.exists)

  // TODO:
  let messageName step = // type' action entity = 
    let entity' = String.IsNullOrEmpty(step.logicalName) |> function
        | true -> "any Entity" | false -> step.logicalName
    sprintf "%s: %s of %s" step.className step.eventOperation entity'

  module Validation =
      
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

      findInvalid plugins invalids "%s: Pre execution stages does not support pre-images"

    let postOperationNoAsync plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,pl) -> 
          pl.step.executionMode = int ExecutionMode.Asynchronous && 
            pl.step.executionStage <> int ExecutionStage.PostOperation)

      findInvalid plugins invalidPlugins "%s: Post execution stages does not support asynchronous execution mode"

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
            
      findInvalid plugins invalidPlugins "%s can't have filtered attributes"

    let associateDisassociateNoImages plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,pl) ->
          not (Seq.isEmpty pl.images))
            
      findInvalid plugins invalidPlugins "%s can't have images"

    let associateDisassociateAllEntity plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,pl) ->
          pl.step.logicalName <> "")
            
      findInvalid plugins invalidPlugins "%s must target all entities"

    let preEventsNoPreImages plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,pl) ->
          let i' = 
            pl.images
            |> Seq.filter(fun image -> image.imageType = int ImageType.PreImage)
          pl.step.eventOperation = "Create" &&
          not (Seq.isEmpty i'))
            
      findInvalid plugins invalidPlugins "%s: Pre-events does not support pre-images"

    let postEventsNoPostImages plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,pl) ->
          let i' = 
            pl.images
            |> Seq.filter(fun image -> image.imageType = int ImageType.PostImage)
          pl.step.eventOperation = "Delete" &&
          not (Seq.isEmpty i'))
            
      findInvalid plugins invalidPlugins "%s: Post-events does not support post-images"
      
    let validateAssociateDisassosiate =
      associateDisassociateNoFilters
      >> bind associateDisassociateNoImages
      >> bind associateDisassociateAllEntity

    let validate =
      postOperationNoAsync
      >> bind preOperationNoPreImages
      >> bind validateAssociateDisassosiate
      >> bind preEventsNoPreImages
      >> bind postEventsNoPostImages

    let validatePlugins plugins =
      plugins
      |> Seq.map(fun pl -> ((messageName pl.step),pl))
      |> validate

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

  let tupleToType ((a,b,c,d),(e,f,g,h,i),images) = 
    let step = 
      { className = a; executionStage = b; eventOperation = c;
        logicalName = d; deployment = e; executionMode = f;
        name = g; executionOrder = h; filteredAttributes = i; }
    let images' =
      images
      |> Seq.map( fun (j,k,l,m) ->
        { name = j; entityAlias = k;
          imageType = l; attributes = m; } )
    { step = step; images = images' }  

  // TODO:
  let typesAndMessages (asm:Assembly) =
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
              (int * int * string * int * string) * 
                seq<(string * string * int * string)>) seq)
      |> Array.toSeq
      |> Seq.concat
      |> Seq.map( fun x -> tupleToType x )

  // TODO:
  let createAssembly name dll (asm:Assembly) hash =
    let pa = Entity("pluginassembly")
    pa.Attributes.Add("name", name)
    pa.Attributes.Add("content", dll |> fileToBase64)
    pa.Attributes.Add("sourcehash", hash)
    pa.Attributes.Add("isolationmode", OptionSetValue(2)) // sandbox
    pa.Attributes.Add("version", asm.GetName().Version.ToString())
    pa.Attributes.Add("description", "Synced with DAXIF# v." + assemblyVersion())
    pa

  // TODO:
  let updateAssembly' (paid:Guid) dll (asm:Assembly) hash =
    let pa = Entity("pluginassembly")
    pa.Attributes.Add("pluginassemblyid", paid)
    pa.Attributes.Add("content", dll |> fileToBase64)
    pa.Attributes.Add("sourcehash", hash)
    pa.Attributes.Add("version", asm.GetName().Version.ToString())
    pa.Attributes.Add("description", "Synced with DAXIF# v." + assemblyVersion())
    pa

  // TODO:
  let createType (asmId:Guid) (name:string) =
    let pt = Entity("plugintype")
    pt.Attributes.Add("name", name)
    pt.Attributes.Add("typename", name)
    pt.Attributes.Add("friendlyname", Guid.NewGuid().ToString())
    pt.Attributes.Add("pluginassemblyid", EntityReference("pluginassembly",asmId))
    pt.Attributes.Add("description", "Synced with DAXIF# v." + assemblyVersion())
    pt

  // TODO:
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
    ps.Attributes.Add("description", "Synced with DAXIF# v." + assemblyVersion())
    String.IsNullOrEmpty(step.logicalName) |> function
      | true  -> ()
      | false ->
        ps.Attributes.Add("sdkmessagefilterid",
          EntityReference("sdkmessagefilter",filterId))
    ps

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
    ps.Attributes.Add("description", "Synced with DAXIF# v." + assemblyVersion())
    ps

  let updateImage (pmid:Guid) image = 
    let psi = Entity("sdkmessageprocessingstepimage")
    psi.Attributes.Add("sdkmessageprocessingstepimageid", pmid)
    psi.Attributes.Add("name", image.name)
    psi.Attributes.Add("entityalias", image.entityAlias)
    psi.Attributes.Add("imagetype", OptionSetValue(image.imageType))
    psi.Attributes.Add("attributes", image.attributes)
    psi

  // Create plugin functions
  let createTypes (log:ConsoleLogger.ConsoleLogger) m tc entitySet assemblyId =
    entitySet 
    |> Set.toArray
    |> Array.Parallel.iter(fun x ->

      use p = ServiceProxy.getOrganizationServiceProxy m tc
      let pt = createType assemblyId x

      log.WriteLine(LogLevel.Verbose, 
        sprintf "Creating type: %s" (getName pt))
      (pt, CrmData.CRUD.create p pt (ParameterCollection()))
      |> fun (x,_) -> 
        log.WriteLine(LogLevel.Verbose, 
          sprintf "%s: %s was created" x.LogicalName (getName x)))
                  

  let createPluginSteps (log:ConsoleLogger.ConsoleLogger) (solution:Entity) 
    m tc entitySet plugins (pluginType: Entity) = 
      entitySet |> Set.toArray
      |> Array.Parallel.map(fun (_,y) ->

        use p = ServiceProxy.getOrganizationServiceProxy m tc

        //let (type',stage,action,entity),(deploy,mode,_,order,fAttributes),_ = 
        let step = 
          plugins
          |> Seq.filter(fun pl -> y = messageName pl.step)
          |> Seq.head
          |> (fun pl -> pl.step)

        let sdkm = CrmData.Entities.retrieveSdkMessage p step.eventOperation
        let sdkf =
          CrmData.Entities.retrieveSdkMessageFilter
            p step.logicalName sdkm.Id

        let ps =
          createStep pluginType.Id sdkm.Id sdkf.Id 
            (messageName step) step

        let pc = ParameterCollection()
        pc.Add("SolutionUniqueName", getAttribute "uniquename" solution)

        ps, CrmData.CRUD.create p ps pc)
      |> Array.iter(
        fun (x,_) -> 
          log.WriteLine(LogLevel.Verbose,
            sprintf "%s: (%O) was created" x.LogicalName (getName x)))

  let createPluginImages (log:ConsoleLogger.ConsoleLogger) (solution:Entity) 
    m tc entitySet plugins (pluginStep: Entity) = 
      entitySet |> Set.toArray
      |> Array.Parallel.iter(fun y ->
           
        use p = ServiceProxy.getOrganizationServiceProxy m tc
                    
        plugins
        |> Seq.fold(fun acc pl -> 
          pl.images
          |> Seq.map(fun image -> (pl.step,image))
          |> Seq.append acc) Seq.empty
        |> Seq.filter(fun (_,image) -> y = image.name)
        |> Seq.toArray
        |> Array.map(fun (step,image) -> 
            let pm =
                createImage pluginStep.Id step.eventOperation image

            let pc = ParameterCollection()
            pc.Add("SolutionUniqueName", getAttribute "uniquename" solution)

            pm, CrmData.CRUD.create p pm pc)

        |> Array.iter(
            fun (x,id) -> 
                log.WriteLine(LogLevel.Verbose,
                    sprintf "%s: (%O) was created" x.LogicalName id)))

  let defaultAttributeVal (e:Entity) key (def:'a) =
    match e.Attributes.TryGetValue(key) with
    | (true,v) -> v :?> 'a
    | (false,_) -> def

  // Update plugin functions
  let updatePluginSteps (log:ConsoleLogger.ConsoleLogger) 
    m tc entitySet plugins =
      entitySet ||> subset'
      |> Seq.toArray
      |> Array.Parallel.map(fun (_,y) ->
        
        use p = ServiceProxy.getOrganizationServiceProxy m tc

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
            
        let step, update =
          plugins
          |> Seq.filter(fun pl -> name = messageName pl.step)
          |> Seq.head
          |> fun pl -> 
            (pl.step,(pl.step.executionStage,pl.step.deployment,pl.step.executionMode,
              pl.step.executionOrder,pl.step.filteredAttributes))

        match (stage.Value,deploy.Value,mode.Value,order,filteredA) = update with
        | true ->  None
        | false ->
          let stepEntity = updateStep y.Id step
          CrmData.CRUD.update p stepEntity |> ignore
          Some y )
      |> Array.choose(id)
      |> Array.iter(
          fun x -> 
              log.WriteLine(LogLevel.Verbose,
                  sprintf "%s: %s was updated" x.LogicalName (getName x)))

  let updatePluginImages (log:ConsoleLogger.ConsoleLogger) m tc entitySet plugins =
      entitySet ||> subset
      |> Seq.toArray
      |> Array.Parallel.map(fun y ->

        use p = ServiceProxy.getOrganizationServiceProxy m tc

        let compareImage =
          let name = getName y
          let alias = getAttribute "entityalias" y :?> string
          let imageType = 
            defaultAttributeVal y "imagetype" (OptionSetValue(0))
          let attributes = defaultAttributeVal y "attributes" null
          {name = name; entityAlias = alias; imageType = imageType.Value; attributes = attributes}

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
          Some y)

      |> Array.choose(id)
      |> Array.iter(
        fun x -> 
          log.WriteLine(LogLevel.Verbose,
            sprintf "%s: (%O) was updated" x.LogicalName x.Id)) 

  // Delete plugin functions
  let deleteTypes (log:ConsoleLogger.ConsoleLogger) m tc entitySet = 
    entitySet ||> subset
    |> Seq.toArray
    |> Array.Parallel.map(fun x ->
      use p = ServiceProxy.getOrganizationServiceProxy m tc
      x, CrmData.CRUD.delete p x.LogicalName x.Id)
    |> Array.iter(
      fun (x,_) -> 
        log.WriteLine(LogLevel.Verbose,
          sprintf "%s: %s was deleted" x.LogicalName (getName x)))

  let deleteSteps (log:ConsoleLogger.ConsoleLogger) m tc entitySet = 
    entitySet ||> subset'
    |> Seq.toArray
    |> Array.Parallel.map(fun (_,y) -> 
      use p = ServiceProxy.getOrganizationServiceProxy m tc
      y, CrmData.CRUD.delete p y.LogicalName y.Id)
    |> Array.iter(
      fun (x,_) -> 
      log.WriteLine(LogLevel.Verbose,
        sprintf "%s: %s was deleted" x.LogicalName (getName x)))

  let deleteImages (log:ConsoleLogger.ConsoleLogger) m tc entitySet = 
    entitySet ||> subset
    |> Seq.toArray
    |> Array.Parallel.map(fun y -> 
      use p = ServiceProxy.getOrganizationServiceProxy m tc
      y, CrmData.CRUD.delete p y.LogicalName y.Id)
    |> Array.iter(
      fun (x,_) -> 
      log.WriteLine(LogLevel.Verbose,
        sprintf "%s: (%O) was deleted" x.LogicalName x.Id))
    
  let instantiateAssembly (solution:Entity) dllName dllPath asm hash p 
    (log:ConsoleLogger.ConsoleLogger) =
      log.WriteLine(LogLevel.Verbose, "Retrieving assemblies from CRM")
      let dlls = CrmData.Entities.retrievePluginAssemblies p solution.Id

      let matchingAssembly = 
        dlls
        |> Seq.filter(fun x -> dllName = getName x)
        |> Seq.map(fun x -> x.Id)
        |> fun x -> if Seq.isEmpty x then None else x |> Seq.head |> Some

      match matchingAssembly with
      | None ->

        log.WriteLine(LogLevel.Info, "Creating Assembly")
        let pa = createAssembly dllName dllPath asm hash
        let pc = ParameterCollection()

        pc.Add("SolutionUniqueName", getAttribute "uniquename" solution)
        let guid = CrmData.CRUD.create p pa pc

        log.WriteLine(LogLevel.Verbose,
            sprintf "%s: (%O) was created" pa.LogicalName guid)
        guid
      | Some id -> 
        id

  let deletePluginImages (asm, (asmId:Guid), (dllName:string), (dllPath:string),
                            (hash:string), (solution:Entity), m, tc, 
                              (log:ConsoleLogger.ConsoleLogger), sourcePlugins) = 
    
    log.WriteLine(LogLevel.Info, "Retrieving Steps")

    use p = ServiceProxy.getOrganizationServiceProxy m tc
    let steps = CrmData.Entities.retrieveAllPluginProcessingSteps p solution.Id
    log.WriteLine(LogLevel.Debug, 
      sprintf "Found %d steps" (Seq.length steps))

    //Delete images
    log.WriteLine(LogLevel.Info, "Deleting images")
    steps
    |> Seq.toArray
    |> Array.Parallel.iter(fun step ->

      use p' = ServiceProxy.getOrganizationServiceProxy m tc

      log.WriteLine(LogLevel.Debug, 
        sprintf "Retrieving images for step: %s" (getName step))
      let images =
          CrmData.Entities.retrievePluginProcessingStepImages 
              p' step.Id
      log.WriteLine(LogLevel.Debug, 
        sprintf "Found %d images" (Seq.length images))
                    
      let sourceImage =
        sourcePlugins
        |> Seq.map(fun pl -> pl.images)
        |> Seq.fold(fun acc i' -> 
          i' 
          |> Seq.map(fun i -> i.name) 
          |> Seq.append acc
        ) Seq.empty
        |> Set.ofSeq

      let targetImage = 
          images
          |> Seq.map(fun y ->
            let name  = getName y
            name)
          |> Set.ofSeq

      let obsoleteImage = targetImage - sourceImage

      deleteImages log m tc (obsoleteImage,images))

    asm, asmId, dllName, dllPath, hash, solution, m, tc, log, sourcePlugins, steps

  let deletePluginSteps (asm, (asmId:Guid), (dllName:string), (dllPath:string),
                          (hash:string), (solution:Entity), m, tc, 
                            (log:ConsoleLogger.ConsoleLogger), sourcePlugins, steps) = 

    //Delete Steps
    let targetSteps =
      steps
      |> Seq.map(fun (x:Entity) -> 
        let stage = getAttribute "stage" x :?> OptionSetValue
        stage.Value, x)
      |> Array.ofSeq |> Seq.ofArray

    let sourceStep =
      sourcePlugins
      |> Seq.map(fun pl -> pl.step.executionStage ,messageName pl.step)
      |> Set.ofSeq

    let targetStep' = 
      targetSteps
      |> Seq.map(fun (x,y) ->
        let name  = getName y
        x, name)
      |> Set.ofSeq

    let obsoleteStep = targetStep' - sourceStep
    log.WriteLine(LogLevel.Info, "Deleting steps")
    deleteSteps log m tc (obsoleteStep, targetSteps)

    asm, asmId, dllName, dllPath, hash, solution, m, tc, log, sourcePlugins
      
  let deletePluginTypes (asm, (asmId:Guid), (dllName:string), (dllPath:string),
                          (hash:string), (solution:Entity), m, tc, 
                            (log:ConsoleLogger.ConsoleLogger), sourcePlugins) = 

    log.WriteLine(LogLevel.Info, "Deleting types")

    use p = ServiceProxy.getOrganizationServiceProxy m tc
    
    log.WriteLine(LogLevel.Debug, "Retrieving types")
    let types = CrmData.Entities.retrievePluginTypes p asmId
    log.WriteLine(LogLevel.Debug, 
      sprintf "Retrieved %d types" (Seq.length types))

    let sourceTypes = 
      sourcePlugins
      |> Seq.map(fun pl -> pl.step.className)
      |> Set.ofSeq
    let targetTypes = 
      types
      |> Seq.map getName
      |> Set.ofSeq

    let newTypes  = sourceTypes - targetTypes
    let obsoleteTypes  = targetTypes - sourceTypes
            
    deleteTypes log m tc (obsoleteTypes,types) 

    asm, asmId, dllName, dllPath, hash, solution, m, tc, log, sourcePlugins, newTypes

  let updateAssembly (asm, (asmId:Guid), (dllName:string), (dllPath:string),
                      (hash:string), (solution:Entity), m, tc, 
                        (log:ConsoleLogger.ConsoleLogger), sourcePlugins, newTypes) =

    log.WriteLine(LogLevel.Verbose, "Retrieving assemblies from CRM")

    use p = ServiceProxy.getOrganizationServiceProxy m tc
    let dlls = CrmData.Entities.retrievePluginAssemblies p solution.Id

    dlls
    |> Seq.filter(fun x -> dllName = getName x)
    |> Seq.iter(fun x ->
      match hash = (getAttribute "sourcehash" x :?> string) with
      | true -> ()
      | false -> 
        log.WriteLine(LogLevel.Info, "Updating Assembly")
        CrmData.CRUD.update
            p (updateAssembly' x.Id dllPath asm hash) |> ignore

        log.WriteLine(LogLevel.Verbose,
          sprintf "%s: %s was updated" x.LogicalName (getName x)))

    asmId, solution, m, tc, log, sourcePlugins, newTypes
  
  let syncTypes (asmId, solution, m, tc, (log:ConsoleLogger.ConsoleLogger), sourcePlugins, newTypes) =
    // Create Plugin Types
    log.WriteLine(LogLevel.Info, "Creating types")
    createTypes log m tc newTypes asmId

    asmId, solution, m, tc, log, sourcePlugins

  let syncSteps (asmId, (solution:Entity), m, tc, (log:ConsoleLogger.ConsoleLogger), sourcePlugins) =

    log.WriteLine(LogLevel.Info, sprintf "Creating and updating steps")

    // Create and update Plugin Steps
    sourcePlugins
    |> Seq.groupBy(fun pl -> pl.step.className)
    |> Seq.toArray
    |> Array.Parallel.iter(
      fun (key,values) -> 

      use p = ServiceProxy.getOrganizationServiceProxy m tc

      log.WriteLine(LogLevel.Debug, 
        sprintf "Retrieving plugin type: %s" key)
      let pt = CrmData.Entities.retrievePluginType p key

      log.WriteLine(LogLevel.Debug, 
        sprintf "Retrieving steps for type : %s" key)
      let steps =
        CrmData.Entities.retrievePluginProcessingSteps
            p pt.Id
        |> Seq.map(fun x -> 
          let stage = getAttribute "stage" x :?> OptionSetValue
          stage.Value, x)
      log.WriteLine(LogLevel.Debug, 
        sprintf "Found %d images" (Seq.length steps))

      let sourceStep =
        values
        |> Seq.map(fun pl -> pl.step.executionStage,messageName pl.step)
        |> Set.ofSeq
      let targetStep = 
        steps
        |> Seq.map(fun (x,y) ->
          let name  = getName y
          x,name)
        |> Set.ofSeq

      let newSteps = sourceStep - targetStep
      let updateSteps = Set.intersect sourceStep targetStep
                
      log.WriteLine(LogLevel.Debug, sprintf "Creating steps for: %s" key)
      createPluginSteps log solution m tc newSteps values pt

      log.WriteLine(LogLevel.Debug, sprintf "Updating steps for: %s" key)
      updatePluginSteps log m tc (updateSteps,steps) values)

    solution, m, tc, log, sourcePlugins

  let syncImages ((solution:Entity), m, tc, (log:ConsoleLogger.ConsoleLogger), sourcePlugins) =

    sourcePlugins
    |> Seq.groupBy(fun pl -> messageName pl.step)
    |> Seq.toArray
    |> Array.Parallel.iter(
      fun (key,plugin)  ->
                
      use p = ServiceProxy.getOrganizationServiceProxy m tc
      log.WriteLine(LogLevel.Debug, 
        sprintf "Retrieving plugin step: %s" key)
      let ps = CrmData.Entities.retrieveSdkProcessingStep p key

      log.WriteLine(LogLevel.Debug, 
        sprintf "Retrieving images for step: %s" key)
      let images =
          CrmData.Entities.retrievePluginProcessingStepImages 
              p ps.Id
      log.WriteLine(LogLevel.Debug, 
        sprintf "Found %d images" (Seq.length images))
                    
      let sourceImage =
        plugin
        |> Seq.map(fun pl -> pl.images)
        |> Seq.fold(fun acc i' -> 
          i' 
          |> Seq.map(fun image -> image.name) 
          |> Seq.append acc) Seq.empty
        |> Set.ofSeq

      let targetImage = 
          images
          |> Seq.map(fun y ->
            let name  = getName y
            name)
          |> Set.ofSeq

      let newImages = sourceImage - targetImage
      let updateImages = Set.intersect sourceImage targetImage

      log.WriteLine(LogLevel.Debug, sprintf "Creating Images for: %s" key)
      createPluginImages log solution m tc newImages plugin ps

      log.WriteLine(LogLevel.Debug, sprintf "Updating Images for: %s" key)
      updatePluginImages log m tc (updateImages,images) plugin)

  let syncPlugins= 

    deletePluginImages
    >> deletePluginSteps
    >> deletePluginTypes
    >> updateAssembly
    >> syncTypes
    >> syncSteps
    >> syncImages
    >> ignore

  let syncSolution' org ac solutionName proj dll (log:ConsoleLogger.ConsoleLogger) =
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

    log.WriteLine(LogLevel.Verbose, "Connecting to CRM")
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc
    let asm = Assembly.LoadFile(dllPath); 
    let dllName = Path.GetFileNameWithoutExtension(dll'); 

    let solution = CrmData.Entities.retrieveSolution p solutionName

    let asmId = instantiateAssembly solution dllName dllPath asm hash p log

    let sourcePlugins = typesAndMessages asm

    log.WriteLine(LogLevel.Verbose, "Validating plugins to be registered")
    match Validation.validatePlugins sourcePlugins with
      | Validation.Invalid x ->
        failwith x
      | Validation.Valid _ -> 
        log.WriteLine(LogLevel.Verbose, "Validation completed")
        (asm, asmId, dllName, dllPath, hash, solution, m, tc, log, sourcePlugins)
        |> syncPlugins 
