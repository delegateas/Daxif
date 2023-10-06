module internal DG.Daxif.Modules.Plugin.Compare

open System
open Microsoft.Xrm.Sdk
open DG.Daxif.Common
open DG.Daxif.Common.Utility

open Domain
open CrmUtility


/// Compares an plugin type from CRM with one in source code
let pluginType (plugin: Plugin) (e: Entity) =
  getRecordName e = plugin.TypeKey


/// Compares an plugin step from CRM with one in source code
let step (step: Step) (x: Entity) =
  let stage = getDefaultOptSetValue x "stage" (int ExecutionStage.Pre)
  let deploy = getDefaultOptSetValue x "supporteddeployment" (int Deployment.ServerOnly)
  let mode = getDefaultOptSetValue x "mode" (int ExecutionMode.Synchronous)
  let order = x.GetAttributeValue<Nullable<int>>("rank").GetValueOrDefault(0)
  let fAttr = x.GetAttributeValue<string>("filteringattributes")
  let user = x.GetAttributeValue<EntityReference>("impersonatinguserid")
  let userId = if isNull user then Guid.Empty else user.Id
  
  let target = (stage, deploy, mode, order, fAttr, userId)
  let source = 
    (step.executionStage, step.deployment, step.executionMode, 
      step.executionOrder, step.filteredAttributes, step.userContext)

  target = source

/// Compares a custom API from CRM with one in source code
let api (message: Message) (x: Entity) =
  let name = x.GetAttributeValue<string>("name")
  let displayname = x.GetAttributeValue<string>("displayname")
  let description = x.GetAttributeValue<string>("description")
  let pluginType = x.GetAttributeValue<EntityReference>("plugintypeid")
  let owner = x.GetAttributeValue<EntityReference>("ownerid")
  let isCustomizable = x.GetAttributeValue<BooleanManagedProperty>("iscustomizable").Value
  let isPrivate =  x.GetAttributeValue<bool>("isprivate")
  let executePrivilegeName = x.GetAttributeValue<string>("executeprivilegename")

  // TODO: Compare owner aswell
  let target = 
    (name, 
    displayname,
    description,
    pluginType.Name,
    isCustomizable,
    isPrivate,
    executePrivilegeName)
  let source = 
    (message.name,
     message.displayName,
     message.description,
     message.pluginTypeName,
     message.isCustomizable,
     message.isPrivate,
     message.executePrivilegeName
     )

  target = source

/// Compares a custom API requestparameter from CRM with one in source code
let apiReq (reqParam: RequestParameter) (x: Entity) =
  let name = x.GetAttributeValue<string>("name")
  let displayname = x.GetAttributeValue<string>("displayname")
  let isCustomizable = x.GetAttributeValue<BooleanManagedProperty>("iscustomizable").Value
  let isOptional = x.GetAttributeValue<bool>("isoptional")
  let logicalEntityName = x.GetAttributeValue<string>("logicalentityname")
  let _type = x.GetAttributeValue<OptionSetValue>("type").Value  
  let customApi = x.GetAttributeValue<EntityReference>("customapiid")

  // TODO: Compare more 
  let target = 
    (name, 
    displayname,
    isCustomizable,
    isOptional,
    logicalEntityName,
    _type,
    customApi.Name
  )
  let source = 
    (reqParam.name,
    reqParam.displayName,
    reqParam.isCustomizable,
    reqParam.isOptional,
    reqParam.logicalEntityName,
    reqParam._type,
    reqParam.customApiName
     )

  target = source

/// Compares a custom API requestparameter from CRM with one in source code
let apiResp (reqParam: ResponseProperty) (x: Entity) =
  let name = x.GetAttributeValue<string>("name")
  let displayname = x.GetAttributeValue<string>("displayname")
  let isCustomizable = x.GetAttributeValue<BooleanManagedProperty>("iscustomizable").Value
  let logicalEntityName = x.GetAttributeValue<string>("logicalentityname")
  let _type = x.GetAttributeValue<OptionSetValue>("type").Value
  let customApi = x.GetAttributeValue<EntityReference>("customapiid")

  // TODO: Compare more 
  let target = 
    (name, 
    displayname,
    isCustomizable,
    logicalEntityName,
    _type,
    customApi.Name
  )
  let source = 
    (reqParam.name,
    reqParam.displayName,
    reqParam.isCustomizable,
    reqParam.logicalEntityName,
    reqParam._type,
    reqParam.customApiName
     )

  target = source

/// Compares an plugin step image from CRM with one in source code
let image (img: Image) (x: Entity) =
  let alias = x.GetAttributeValue<string>("entityalias")
  let imgType = getDefaultOptSetValue x "imagetype" (int ImageType.PreImage)
  let attr = x.GetAttributeValue<string>("attributes")

  let targetCompare = (alias, imgType, attr)
  let sourceCompare = (img.entityAlias, img.imageType, img.attributes)

  targetCompare = sourceCompare

/// Compares a Custom API Request Parameter from CRM with one in source code
// TODO

/// Compares a Custom API Response Property from CRM with one in source code
// TODO


/// Compares an assembly from CRM with the one containing the source code
let assembly (local: AssemlyLocal) (registered: AssemblyRegistration option) =
  registered
  ?|> fun y -> y.hash = local.hash 
  ?| false
