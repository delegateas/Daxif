module internal DG.Daxif.Modules.Plugin.PluginDetection

open System
open System.IO
open System.Reflection
open System.Xml.Linq
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

open Domain

    
/// Used to retrieve a .vsproj dependencies (recursive)
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
      |> Seq.choose (fun elem -> getElemValue "HintPath" elem ?|? getAttrValue "Include" elem)
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

  projDependencies' (Path.GetFullPath(vsproj))

/// Transforms the received tuple from the assembly file through invocation into
/// plugin, step and image records
let tupleToPlugin 
  ((className, stage, eventOp, logicalName),
    (deployment, mode, _, order, fAttr, userId),
    imgTuples) = 

  let entity' = 
    String.IsNullOrEmpty(logicalName) |> function
    | true -> "any Entity" | false -> logicalName
  let execMode = (enum<ExecutionMode> mode).ToString()
  let execStage = (enum<ExecutionStage> stage).ToString()
  let stepName = sprintf "%s: %s %s %s of %s" className execMode execStage eventOp entity'

  let step = 
    { pluginTypeName = className
      executionStage = stage
      eventOperation = eventOp
      logicalName = logicalName
      deployment = deployment
      executionMode = mode
      name = stepName
      executionOrder = order
      filteredAttributes = fAttr
      userContext = Guid.Parse(userId)
    }
    
  let images =
    imgTuples
    |> Seq.map (fun (iName, iAlias, iType, iAttr) ->
      { stepName = stepName
        name = iName
        entityAlias = iAlias
        imageType = iType
        attributes = iAttr
      })

  { step = step
    images = images 
  }

/// Transforms the received tuple from the assembly file through invocation into
/// custom api, request parameter and response property records
let tupleToCustomApi
  ((name, isFunction, enabledForWorkflow, allowedCustomProcessingStepType, bindingType, boundEntityLogicalName),
    (pluginTypeName, ownerId, ownerType, isCustomizable, isPrivate, executePrivilegeName, description),
    reqParams: seq<Tuple<string, string, string, bool, bool, string, int>>, resProps: seq<Tuple<string, string, string, bool, string, int>>) = 
  
  //let entity' = 
  //  String.IsNullOrEmpty(logicalName) |> function
  //  | true -> "any Entity" | false -> logicalName
  //let execMode = (enum<ExecutionMode> mode).ToString()
  //let execStage = (enum<ExecutionStage> stage).ToString()
  //let stepName = sprintf "%s: %s %s %s of %s" className execMode execStage eventOp entity'
  let result = ref Guid.Empty
  let message = 
    { 
      uniqueName = name
      name = name
      displayName = name
      description = description
      isFunction = isFunction
      enabledForWorkflow = enabledForWorkflow
      bindingType = bindingType
      boundEntityLogicalName = boundEntityLogicalName
      allowedCustomProcessingStepType = allowedCustomProcessingStepType
      pluginTypeName = pluginTypeName
      ownerId = if Guid.TryParse(ownerId, result) then result.Value else Guid.Empty // TODO
      ownerType = ownerType
      isCustomizable = isCustomizable
      isPrivate = isPrivate
      executePrivilegeName = executePrivilegeName
    }
    
  let reqParams : RequestParameter seq =
    reqParams
    |> Seq.map (fun (iParam) ->
      let (name, uniqueName, displayName, isCustomizable, isOptional, logicalEntityName, _type) = iParam
      { 
        name = name
        customApiName = message.name
        uniqueName = uniqueName
        displayName = displayName
        isCustomizable = isCustomizable
        isOptional = isOptional
        logicalEntityName = logicalEntityName
        _type = _type
      })

  let resProps =
    resProps
    |> Seq.map (fun (iProp) ->
      let (name, uniqueName, displayName, isCustomizable, logicalEntityName, _type) = iProp
      { 
        name = name
        customApiName = message.name
        uniqueName = uniqueName
        displayName = displayName
        isCustomizable = isCustomizable
        logicalEntityName = logicalEntityName
        _type = _type
      })

  { 
    message = message
    reqParameters = reqParams
    resProperties = resProps
  }  

let getValidPlugins (types:Type[]) = 
  let pluginType = 
    types 
    |> Array.filter (fun x -> x.Name = @"Plugin") 
    |> Array.tryHead

  if pluginType.IsNone then Array.empty else

  let validTypes, invalidTypes = 
      types
      |> Array.filter (fun (x:Type) -> x.IsSubclassOf(pluginType.Value))
      |> Array.map (fun (x:Type) -> 
        let constructorType = 
          match x.GetConstructor(Type.EmptyTypes) <> null, x.GetConstructor([|typeof<string>|]) <> null, x.GetConstructor([|typeof<string>; typeof<string>|]) <> null with
          | true,_,_ -> Some PluginConstructorType.Empty
          | _,true,_ -> Some PluginConstructorType.Unsecure
          | _,_,true -> Some PluginConstructorType.Secure
          | false,false,false -> None

        x,constructorType
      )
      |> Array.partition (fun (x:Type, constructor: PluginConstructorType option) -> 
        not x.IsAbstract && constructor.IsSome
      )
  
  invalidTypes
  |> Array.iter (fun (x:Type, constructor: PluginConstructorType option) -> 
    if x.IsAbstract 
    then log.Warn "The plugin '%s' is an abstract type and is therefore not valid. The plugin will not be synchronized" (x.Name)
    if constructor.IsNone
    then log.Warn "The plugin '%s' does not contain a valid contructor and is therefore not valid. The plugin will not be synchronized" (x.Name)
  )

  validTypes
  |> Array.map (fun (x: Type, constructor: PluginConstructorType option) -> x,constructor.Value)

/// Calls "PluginProcessingStepConfigs" in the plugin assembly that returns a
/// tuple containing the plugin information
let getPluginsFromAssembly (asm: Assembly) =
  try
    getLoadableTypes asm log 
    |> getValidPlugins
    |> fun validPlugins ->

      validPlugins
      |> Array.Parallel.map (fun (x:Type, constructorType: PluginConstructorType) ->
        let instance = 
          match constructorType with
          | Empty -> Activator.CreateInstance(x)
          | Unsecure -> Activator.CreateInstance(x, [|null|])
          | Secure -> Activator.CreateInstance(x, [|null;null|])
          
        instance, x.GetMethod(@"PluginProcessingStepConfigs"))
      |> Array.Parallel.map (fun (x, (y:MethodInfo)) -> 
          y.Invoke(x, [||]) :?> 
            ((string * int * string * string) * 
              (int * int * string * int * string * string) * 
                seq<(string * string * int * string)>) seq)
      |> Array.toSeq
      |> Seq.concat
      |> Seq.map( fun x -> tupleToPlugin x )
  with
  | ex -> 
    failwithf @"Failed to fetch plugin configuration from plugin assembly. This error can be caused if an old version of Plugin.cs is used.\nFull Exception: %s"
      (getFullException(ex))

let getValidCustomAPIs(types:Type[]) = 
  let customApiType = 
    types 
    |> Array.filter (fun x -> x.Name = @"CustomAPI") 
    |> Array.tryHead

  if customApiType.IsNone then Array.empty else

  let validTypes, invalidTypes = 
      types
      |> Array.filter (fun (x:Type) -> x.IsSubclassOf(customApiType.Value))
      |> Array.partition (fun (x:Type) -> not x.IsAbstract && x.GetConstructor(Type.EmptyTypes) <> null)
  
  invalidTypes
  |> Array.iter (fun (x:Type) -> 
    if x.IsAbstract 
    then log.Warn "The custom api '%s' is an abstract type and is therefore not valid. The custom api will not be synchronized" (x.Name)
    if x.GetConstructor(Type.EmptyTypes) = null
    then log.Warn "The custom api '%s' does not contain an empty contructor and is therefore not valid. The custom api will not be synchronized" (x.Name)
  )

  validTypes

/// Calls "GetCustomAPIConfig" in the assembly that returns a
/// tuple containing the custom api information
let getCustomAPIsFromAssembly (asm: Assembly) =
  try
    getLoadableTypes asm log 
    |> getValidCustomAPIs
    |> fun validCustomAPIs ->

      validCustomAPIs
      |> Array.Parallel.map (fun (x:Type) -> 
        Activator.CreateInstance(x), x.GetMethod(@"GetCustomAPIConfig"))
      |> Array.Parallel.map (fun (x, (y:MethodInfo)) -> 
          y.Invoke(x, [||]) :?> 
            (string * bool * int * int * int * string) * 
            (string * string * string * bool * bool * string * string) * 
            seq<Tuple<string, string, string, bool, bool, string, int>> *
            seq<Tuple<string, string, string, bool, string, int>>)
      |> Array.toSeq
      |> Seq.map( fun x -> tupleToCustomApi x )
  with
  | ex -> 
    failwithf @"Failed to fetch custom api configuration from assembly. This error can be caused if an old version of CustomAPI.cs is used.\nFull Exception: %s"
      (getFullException(ex))

/// Analyzes an assembly based on a path to its compiled assembly and its project file
let getAssemblyContextFromDll projectPath dllPath isolationMode ignoreOutdatedAssembly =
  let dllFullPath = Path.GetFullPath(dllPath)
  let dllTempPath = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString() + @".dll")
  let dllName     = Path.GetFileNameWithoutExtension(dllFullPath); 

  File.Copy(dllFullPath, dllTempPath, true)

  let asmWriteTime = File.GetLastWriteTimeUtc dllFullPath
  let hash =
      projDependencies projectPath 
      |> Set.ofSeq
      |> Set.map(fun x -> 
        match not(ignoreOutdatedAssembly) && File.GetLastWriteTimeUtc x > asmWriteTime with
        | true  -> failwithf "A file in the project was updated later than compiled assembly: %s\nPlease recompile and synchronize again, or use the \"ignoreOutdatedAssembly\" option." x
        | false -> File.ReadAllBytes(x) |> sha1CheckSum'
      )
      |> Set.fold (fun a x -> a + x |> sha1CheckSum) String.Empty

  let asm = Assembly.LoadFile(dllTempPath); 
    
  { assembly = asm
    assemblyId = None
    dllName = dllName
    dllPath = dllFullPath
    hash = hash
    isolationMode = isolationMode
    plugins = getPluginsFromAssembly asm
    customAPIs = getCustomAPIsFromAssembly asm
  }
