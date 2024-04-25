module internal DG.Daxif.Modules.Plugin.PluginDetection

open System
open System.IO
open System.Reflection
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

open Domain

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
      |> Array.partition (fun (x:Type) -> not x.IsAbstract && x.GetConstructor(Type.EmptyTypes) <> null)
  
  invalidTypes
  |> Array.iter (fun (x:Type) -> 
    if x.IsAbstract 
    then log.Warn "The plugin '%s' is an abstract type and is therefore not valid. The plugin will not be synchronized" (x.Name)
    if x.GetConstructor(Type.EmptyTypes) = null 
    then log.Warn "The plugin '%s' does not contain an empty contructor and is therefore not valid. The plugin will not be synchronized" (x.Name)
  )

  validTypes

/// Calls "PluginProcessingStepConfigs" in the plugin assembly that returns a
/// tuple containing the plugin information
let getPluginsFromAssembly (asm: Assembly) =
  try
    getLoadableTypes asm log 
    |> getValidPlugins
    |> fun validPlugins ->

      validPlugins
      |> Array.Parallel.map (fun (x:Type) -> 
        Activator.CreateInstance(x), x.GetMethod(@"PluginProcessingStepConfigs"))
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
let getAssemblyContextFromDll dllPath isolationMode =
  let dllFullPath = Path.GetFullPath(dllPath)
  let dllTempPath = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString() + @".dll")
  let dllName     = Path.GetFileNameWithoutExtension(dllFullPath); 

  File.Copy(dllFullPath, dllTempPath, true)
  let hash = File.ReadAllBytes dllPath |> sha1CheckSum'
  let asm = Assembly.LoadFile(dllTempPath); 
  let version = asm.GetName().Version |> fun y -> (y.Major, y.Minor, y.Build, y.Revision)
  
  { assembly = asm
    assemblyId = None
    dllName = dllName
    dllPath = dllFullPath
    hash = hash
    version = version
    isolationMode = isolationMode
    plugins = getPluginsFromAssembly asm
    customAPIs = getCustomAPIsFromAssembly asm
  }
