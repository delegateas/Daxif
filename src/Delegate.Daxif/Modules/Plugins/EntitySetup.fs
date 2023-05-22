module internal DG.Daxif.Modules.Plugin.EntitySetup

open System
open System.Reflection
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

open Domain
open CrmUtility


/// Used to set the requied attribute messagePropertyName 
/// based on the message class when creating images
let propertyNames =
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


/// Creates a new assembly in CRM with the provided information
let createAssembly name pathToDll (asm: Assembly) hash (isolationMode: AssemblyIsolationMode) =
  let pa = Entity("pluginassembly")
  pa.Attributes.Add("name", name)
  pa.Attributes.Add("content", pathToDll |> fileToBase64)
  pa.Attributes.Add("sourcehash", hash)
  pa.Attributes.Add("isolationmode", OptionSetValue(int isolationMode)) // sandbox OptionSetValue(2)
  pa.Attributes.Add("version", asm.GetName().Version.ToString())
  pa.Attributes.Add("description", syncDescription())
  pa

/// Create a new type in CRM under the defined assembly id
let createType (asmId:Guid) (name:string) =
  let pt = Entity("plugintype")
  pt.Attributes.Add("name", name)
  pt.Attributes.Add("typename", name)
  pt.Attributes.Add("friendlyname", Guid.NewGuid().ToString())
  pt.Attributes.Add("pluginassemblyid", EntityReference("pluginassembly",asmId))
  pt.Attributes.Add("description", syncDescription())
  pt

/// Create a new step with the provided step information in CRM under the defined type
let createStep (typeId:Guid) (messageId:Guid) (filterId:Guid) name step =
  let ps = Entity("sdkmessageprocessingstep")
  ps.Attributes.Add("name", name)
  ps.Attributes.Add("asyncautodelete", false)
  ps.Attributes.Add("rank", step.executionOrder)
  ps.Attributes.Add("mode", OptionSetValue(step.executionMode))
  ps.Attributes.Add("plugintypeid", EntityReference("plugintype", typeId))
  ps.Attributes.Add("sdkmessageid", EntityReference("sdkmessage", messageId))
  ps.Attributes.Add("stage", OptionSetValue(step.executionStage))
  ps.Attributes.Add("filteringattributes", step.filteredAttributes)
  ps.Attributes.Add("supporteddeployment", OptionSetValue(step.deployment))
  ps.Attributes.Add("description", syncDescription())
  match guidNotSet step.userContext with
    | true -> ()
    | false -> ps.Attributes.Add("impersonatinguserid", EntityReference("systemuser", step.userContext))
  String.IsNullOrEmpty(step.logicalName) |> function
    | true  -> ()
    | false ->
      ps.Attributes.Add("sdkmessagefilterid",
        EntityReference("sdkmessagefilter",filterId))
  ps

/// Create a new image with the provided image informations under the defined step
let createImage (stepId:Guid) eventOperation (image:Image) =
  if not <| propertyNames.ContainsKey eventOperation then
    failwithf "Could not create step images since event operation '%s' was not recognized." eventOperation

  let psi = Entity("sdkmessageprocessingstepimage")
  psi.Attributes.Add("name", image.name)
  psi.Attributes.Add("entityalias", image.entityAlias)
  psi.Attributes.Add("imagetype", OptionSetValue(image.imageType))
  psi.Attributes.Add("attributes", image.attributes)
  psi.Attributes.Add("messagepropertyname", propertyNames.[eventOperation])
  psi.Attributes.Add("sdkmessageprocessingstepid", EntityReference("sdkmessageprocessingstep", stepId))
  psi

/// Used to update an existing step with changes to its attributes
/// Only check for updated on stage, deployment, mode, rank and filteredAttributes. 
/// The rest must be update by UI
let updateStep (stepId:Guid) step =
  let ps = Entity("sdkmessageprocessingstep")
  ps.Attributes.Add("sdkmessageprocessingstepid", stepId)
  ps.Attributes.Add("stage", OptionSetValue(step.executionStage))
  ps.Attributes.Add("filteringattributes", step.filteredAttributes)
  ps.Attributes.Add("supporteddeployment", OptionSetValue(step.deployment))
  ps.Attributes.Add("mode", OptionSetValue(step.executionMode))
  ps.Attributes.Add("rank", step.executionOrder)
  ps.Attributes.Add("description", syncDescription())
  match guidNotSet step.userContext with
    | true -> ps.Attributes.Add("impersonatinguserid", null)
    | false -> ps.Attributes.Add("impersonatinguserid", EntityReference("systemuser", step.userContext))
  ps

/// Used to update an existing image with changes to its attributes
let updateImage (sourceImage:Image) (targetImageEntity:Entity) = 
  let psi = Entity("sdkmessageprocessingstepimage")
  psi.Attributes.Add("sdkmessageprocessingstepimageid", targetImageEntity.Id)
  psi.Attributes.Add("name", sourceImage.name)
  psi.Attributes.Add("entityalias", sourceImage.entityAlias)
  psi.Attributes.Add("imagetype", OptionSetValue(sourceImage.imageType))
  psi.Attributes.Add("attributes", sourceImage.attributes)
  psi.Attributes.Add("sdkmessageprocessingstepid", targetImageEntity.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid"))
  psi


/// Create a new customAPI with the provided CustomAPI
let createCustomAPI (api:Message) (pluginType: EntityReference) (prefix: string) =
  let newCustomAPI = Entity("customapi")
  newCustomAPI.Attributes.Add("allowedcustomprocessingsteptype", OptionSetValue(api.allowedCustomProcessingStepType))
  newCustomAPI.Attributes.Add("bindingtype", OptionSetValue(api.bindingType))
  newCustomAPI.Attributes.Add("boundentitylogicalname", api.boundEntityLogicalName)
  newCustomAPI.Attributes.Add("description", syncDescription())
  newCustomAPI.Attributes.Add("displayname", api.displayName)
  newCustomAPI.Attributes.Add("executeprivilegename", api.executePrivilegeName)
  newCustomAPI.Attributes.Add("isfunction", api.isFunction)
  newCustomAPI.Attributes.Add("isprivate", api.isPrivate)
  newCustomAPI.Attributes.Add("name", api.name)
  newCustomAPI.Attributes.Add("plugintypeid", pluginType)
  newCustomAPI.Attributes.Add("uniquename", prefix + "_" + api.uniqueName)
  newCustomAPI.Attributes.Add("iscustomizable",BooleanManagedProperty(api.isCustomizable))
  newCustomAPI

/// Create a new customAPIReq
let createCustomAPIReq (req:RequestParameter) (customapi: EntityReference) (prefix: string) =
  let newReq = Entity("customapirequestparameter")

  newReq.Attributes.Add("description", syncDescription())
  newReq.Attributes.Add("displayname", req.displayName)
  newReq.Attributes.Add("logicalentityname", req.logicalEntityName)
  newReq.Attributes.Add("isoptional", req.isOptional)
  newReq.Attributes.Add("name", req.name)
  newReq.Attributes.Add("type", OptionSetValue(req._type))
  newReq.Attributes.Add("uniquename", req.uniqueName)
  newReq.Attributes.Add("iscustomizable",BooleanManagedProperty(req.isCustomizable))
  newReq.Attributes.Add("customapiid", customapi)
  newReq

/// Create a new customAPIResp
let createCustomAPIResp (resp:ResponseProperty) (customapi: EntityReference) (prefix: string) =
  let newResp = Entity("customapiresponseproperty")
  newResp.Attributes.Add("description", syncDescription())
  newResp.Attributes.Add("displayname", resp.displayName)
  newResp.Attributes.Add("logicalentityname", resp.logicalEntityName)
  newResp.Attributes.Add("name", resp.name)
  newResp.Attributes.Add("type", OptionSetValue(resp._type))
  newResp.Attributes.Add("uniquename", resp.uniqueName)
  newResp.Attributes.Add("iscustomizable",BooleanManagedProperty(resp.isCustomizable))
  newResp.Attributes.Add("customapiid", customapi)
  newResp

 