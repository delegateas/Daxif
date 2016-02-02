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

  // Helpers functions
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
  let subset' (xs:(int*string) Set) (ys:(int*Entity) seq) = 
    ys
    |> Seq.filter(fun (y,y') -> 
      let z = getName y'
      ((fun x -> x = (y,z)),xs) ||> Seq.exists)

  // TODO:
  let messageName type' action entity = 
    let entity' = String.IsNullOrEmpty(entity) |> function
        | true -> "any Entity" | false -> entity
    sprintf "%s: %s of %s" type' action entity'

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

    let bind switchFunction = 
      function
      | Valid s -> switchFunction s
      | Invalid f -> Invalid f
           
    let findInvalid plugins invalidPlugins msg =
      match invalidPlugins |> Seq.tryPick Some with
        | Some(name,_) -> Invalid (sprintf msg name)
        | None -> Valid plugins

      
    let preOperationNoPreImages plugins =
      let invalids =
        plugins
        |> Seq.filter(fun (_,(_,stage,_,_,_,i)) ->
          let i' = 
            i
            |> Seq.filter(fun (_,_,t,_) -> t = int ImageType.PostImage)
          (stage = int ExecutionStage.PreOperation || 
            stage = int ExecutionStage.PreValidation) &&
              not (Seq.isEmpty i'))

      findInvalid plugins invalids "%s: Pre execution stages does not support pre-images"

    let postOperationNoAsync plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,(_,stage,_,mode,_,_)) -> 
          mode = int ExecutionMode.Asynchronous && 
            stage <> int ExecutionStage.PostOperation)

      findInvalid plugins invalidPlugins "%s: Post execution stages does not support asynchronous execution mode"

    let associateDisasociateSteps plugins =
      plugins
      |> Seq.filter(fun (_,(_,_,event,_,_,_)) -> 
        event = "Associate" ||
        event = "Disassociate")
        
    let associateDisassociateNoFilters plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,(_,_,_,_,filters,_)) ->
          filters <> null)
            
      findInvalid plugins invalidPlugins "%s can't have filtered attributes"

    let associateDisassociateNoImages plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,(_,_,_,_,_,i)) ->
          not (Seq.isEmpty i))
            
      findInvalid plugins invalidPlugins "%s can't have images"

    let associateDisassociateAllEntity plugins =
      let invalidPlugins =
        plugins
        |> associateDisasociateSteps
        |> Seq.filter(fun (_,(ln,_,_,_,_,_)) ->
          ln <> "")
            
      findInvalid plugins invalidPlugins "%s must target all entities"

    let preEventsNoPreImages plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,(_,_,event,_,_,i)) ->
          let i' = 
            i
            |> Seq.filter(fun (_,_,t,_) -> t = int ImageType.PreImage)
          event = "Create" &&
          not (Seq.isEmpty i'))
            
      findInvalid plugins invalidPlugins "%s: Pre-events does not support pre-images"

    let postEventsNoPostImages plugins =
      let invalidPlugins =
        plugins
        |> Seq.filter(fun (_,(_,_,event,_,_,i)) ->
          let i' = 
            i
            |> Seq.filter(fun (_,_,t,_) -> t = int ImageType.PostImage)
          event = "Delete" &&
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
      let plugins' =
        plugins
        |> Seq.map(fun ((cn,es,eo,ln),(_,em,_,_,fa),img) -> ((messageName cn eo ln),(ln,es,eo,em,fa,img)))
      plugins'
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
  let createStep (typeId:Guid) (messageId:Guid) (filterId:Guid)
    name stage entity mode deploy order fAttributes =

    let ps = Entity("sdkmessageprocessingstep")
    ps.Attributes.Add("name", name)
    ps.Attributes.Add("asyncautodelete", false)
    ps.Attributes.Add("rank", order)
    ps.Attributes.Add("mode", OptionSetValue(mode))
    ps.Attributes.Add("plugintypeid", EntityReference("plugintype",typeId))
    ps.Attributes.Add("sdkmessageid", EntityReference("sdkmessage",messageId))
    ps.Attributes.Add("stage", OptionSetValue(stage))
    ps.Attributes.Add("filteringattributes", fAttributes)
    ps.Attributes.Add("supporteddeployment", OptionSetValue(deploy))
    ps.Attributes.Add("description", "Synced with DAXIF# v." + assemblyVersion())
    String.IsNullOrEmpty(entity) |> function
      | true  -> ()
      | false ->
        ps.Attributes.Add("sdkmessagefilterid",
          EntityReference("sdkmessagefilter",filterId))
    ps

  let createImage (stepId:Guid) stepName
    name alias imageType attributes =
    let psi = Entity("sdkmessageprocessingstepimage")
    psi.Attributes.Add("name", name)
    psi.Attributes.Add("entityalias", alias)
    psi.Attributes.Add("imagetype", OptionSetValue(imageType))
    psi.Attributes.Add("attributes", attributes)
    psi.Attributes.Add("messagepropertyname", propertyName.[stepName])
    psi.Attributes.Add("sdkmessageprocessingstepid", 
      EntityReference("sdkmessageprocessingstep",stepId))
    psi

  // Only check for updated on stage, deployment, mode, rank and filteredAttributes. 
  // The rest must be update by UI
  let updateStep (pmid:Guid) (stage, deploy, mode, order,filteredA) =
    let ps = Entity("sdkmessageprocessingstep")
    ps.Attributes.Add("sdkmessageprocessingstepid", pmid)
    ps.Attributes.Add("stage", OptionSetValue(stage))
    ps.Attributes.Add("filteringattributes", filteredA)
    ps.Attributes.Add("supporteddeployment", OptionSetValue(deploy))
    ps.Attributes.Add("mode", OptionSetValue(mode))
    ps.Attributes.Add("rank", order)
    ps.Attributes.Add("description", "Synced with DAXIF# v." + assemblyVersion())
    ps

  let updateImage (pmid:Guid) (name, alias, imageType, attributes) =
    let psi = Entity("sdkmessageprocessingstepimage")
    psi.Attributes.Add("sdkmessageprocessingstepimageid", pmid)
    psi.Attributes.Add("name", name)
    psi.Attributes.Add("entityalias", alias)
    psi.Attributes.Add("imagetype", OptionSetValue(imageType))
    psi.Attributes.Add("attributes", attributes)
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
    m tc entitySet values (pluginType: Entity) = 
      entitySet |> Set.toArray
      |> Array.Parallel.map(fun (_,y) ->

        use p = ServiceProxy.getOrganizationServiceProxy m tc

        let (type',stage,action,entity),(deploy,mode,_,order,fAttributes),_ = 
          values
          |> Seq.filter(fun ((t',_,a',e'),_,_) -> y = messageName t' a' e')
          |> Seq.head

        let sdkm = CrmData.Entities.retrieveSdkMessage p action
        let sdkf =
          CrmData.Entities.retrieveSdkMessageFilter
            p entity sdkm.Id

        let ps =
          createStep pluginType.Id sdkm.Id sdkf.Id 
            (messageName type' action entity) 
              stage entity mode deploy order fAttributes

        let pc = ParameterCollection()
        pc.Add("SolutionUniqueName", getAttribute "uniquename" solution)

        ps, CrmData.CRUD.create p ps pc)
      |> Array.iter(
        fun (x,_) -> 
          log.WriteLine(LogLevel.Verbose,
            sprintf "%s: (%O) was created" x.LogicalName (getName x)))

  let createPluginImages (log:ConsoleLogger.ConsoleLogger) (solution:Entity) 
    m tc entitySet values (pluginStep: Entity) = 
      entitySet |> Set.toArray
      |> Array.Parallel.iter(fun y ->
           
        use p = ServiceProxy.getOrganizationServiceProxy m tc
            
        values
        |> Seq.map(fun ((_,_,e,_),_,i) -> e,i)
        |> Seq.fold(fun acc (e,i) -> 
          i 
          |> Seq.map(fun (n,ea,it,a) -> (e,n,ea,it,a))
          |> Seq.append acc) Seq.empty
        |> Seq.filter(fun (_,n,_,_,_) -> y = n)
        |> Seq.toArray
        |> Array.map(fun (e,n,al,t,at) -> 
            
            let pm =
                createImage pluginStep.Id e n al t at

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
    m tc entitySet values =
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
            
        let updates =
          values
          |> Seq.filter(fun ((t,_,a,e),_,_) -> name = messageName t a e)
          |> Seq.head
          |> fun ((_,stage,_,_),(deploy,mode,_,order,filteredA),_) -> 
            (stage,deploy,mode,order,filteredA)

        match (stage.Value,deploy.Value,mode.Value,order,filteredA) = updates with
        | true ->  None
        | false ->
          let ps = updateStep y.Id updates
          CrmData.CRUD.update p ps |> ignore
          Some y)
      |> Array.choose(id)
      |> Array.iter(
          fun x -> 
              log.WriteLine(LogLevel.Verbose,
                  sprintf "%s: %s was updated" x.LogicalName (getName x)))

  let updatePluginImages (log:ConsoleLogger.ConsoleLogger) m tc entitySet values =
      entitySet ||> subset
      |> Seq.toArray
      |> Array.Parallel.map(fun y ->

        use p = ServiceProxy.getOrganizationServiceProxy m tc
        let name = getName y
        let alias = getAttribute "entityalias" y :?> string
        let imageType = 
          defaultAttributeVal y "imagetype" (OptionSetValue(0))
        let attribute = defaultAttributeVal y "attributes" null

        let updates =
          values
          |> Seq.map(fun (_,_,i) -> i)
          |> Seq.fold(fun acc i' -> 
            i' 
            |> Seq.append acc) Seq.empty
          |> Seq.filter(fun (n,_,_,_) -> name = n)
          |> Seq.head

        match (name,alias,imageType.Value,attribute) = updates with
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
        |> Seq.map(fun (_,_,i) -> i)
        |> Seq.fold(fun acc i' -> 
          i' 
          |> Seq.map(fun (n,_,_,_) -> n) 
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
      |> Seq.map(fun ((t,s,a,e),_,_) -> s,messageName t a e)
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
      |> Seq.map(fun ((type',_,_,_),_,_) -> type')
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
    |> Seq.groupBy(fun ((type',_,_,_),_,_) -> type')
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
        |> Seq.map(fun ((t,s,a,e),_,_) -> s,messageName t a e)
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
    |> Seq.groupBy(fun ((t,_,a,e),_,_) -> messageName t a e)
    |> Seq.toArray
    |> Array.Parallel.iter(
      fun (key,values)  ->
                
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
        values
        |> Seq.map(fun (_,_,i) -> i)
        |> Seq.fold(fun acc i' -> 
          i' 
          |> Seq.map(fun (n,_,_,_) -> n) 
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
      createPluginImages log solution m tc newImages values ps

      log.WriteLine(LogLevel.Debug, sprintf "Updating Images for: %s" key)
      updatePluginImages log m tc (updateImages,images) values)

  let syncPlugins : (_*_*_*_*_*_*_*_*ConsoleLogger.ConsoleLogger*_) -> unit = 

    deletePluginImages
    >> deletePluginSteps
    >> deletePluginTypes
    >> updateAssembly
    >> syncTypes
    >> syncSteps
    >> syncImages

  // TODO:
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
    match sourcePlugins |> Validation.validatePlugins with
      | Validation.Invalid x ->
        failwith x
      | Validation.Valid _ -> 
        log.WriteLine(LogLevel.Verbose, "Validation completed")
        (asm, asmId, dllName, dllPath, hash, solution, m, tc, log, sourcePlugins)
        |> syncPlugins 
