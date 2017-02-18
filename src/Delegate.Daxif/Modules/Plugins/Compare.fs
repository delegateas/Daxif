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
  let deploy = getDefaultOptSetValue x "stage" (int Deployment.ServerOnly)
  let mode = getDefaultOptSetValue x "stage" (int ExecutionMode.Synchronous)
  let order = x.GetAttributeValue<Nullable<int>>("rank").GetValueOrDefault(0)
  let fAttr = x.GetAttributeValue<string>("filteringattributes")
  let user = x.GetAttributeValue<Nullable<Guid>>("impersonatinguserid").GetValueOrDefault(Guid.Empty)
    
  let target = (stage, deploy, mode, order, fAttr, user)
  let source = 
    (step.executionStage, step.deployment, step.executionMode, 
      step.executionOrder, step.filteredAttributes, step.userContext)

  target = source


/// Compares an plugin step image from CRM with one in source code
let image (img: Image) (x: Entity) =
  let name = getRecordName x
  let alias = x.GetAttributeValue<string>("entityalias")
  let imgType = getDefaultOptSetValue x "imagetype" (int ImageType.PreImage)
  let attr = x.GetAttributeValue<string>("attributes")

  let targetCompare = (name, alias, imgType, attr)
  let sourceCompare = (img.name, img.entityAlias, img.imageType, img.attributes)

  targetCompare = sourceCompare


/// Compares an assembly from CRM with the one containing the source code
let assembly (asm: AssemblyContext) (x: Entity option) =
  x
  ?|> fun y -> y.GetAttributeValue<string>("sourcehash") = asm.hash 
  ?| false