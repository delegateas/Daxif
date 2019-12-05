module DG.Daxif.Modules.Solution.SolutionDiffHelper

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages;
open System.Xml
open DG.Daxif.Common
open DG.Daxif.Modules.Solution
open InternalUtility
open Domain
open DiffFetcher
open DiffAdder

let removeNode (node: XmlNode) =
  if node <> null then
    node.ParentNode.RemoveChild node |> ignore

type DiffSolutionInfo = {
  proxy: IOrganizationService
  devExtractedPath: string
  prodExtractedPath: string
  solutionUniqueName: string
}

let diffElement (diffSolutionInfo: DiffSolutionInfo) (devNode: XmlNode) (prodNode: XmlNode) (resp: RetrieveEntityResponse option)
              type_ devNodePath devId (devReadable: ReadableName) (extraCheck: ExtraChecks) (callbackKind: CallbackKind) =

  let { solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  let devElements = selectNodes devNode devNodePath

  devElements |> Seq.map (fun devElement ->
    let id = getId devElement devId
    let name = GetReadableName devElement devReadable
    let prodElement = prodNode.SelectSingleNode(append_selector devNodePath id devId)
    let extraCheckfunction = extraCheckFun (diffSolutionInfo.devExtractedPath, diffSolutionInfo.prodExtractedPath) 
                                            devElement prodElement extraCheck
    
    let callback = callbackFun diffSolutionUniqueName resp
    // remove_useless dev_elem prod_elem;
    if prodElement = null then
      log.Verbose "Adding new %s: %s" type_ name;
      (Some (callback id callbackKind))
    else if devElement.OuterXml = prodElement.OuterXml && extraCheckfunction then
      log.Verbose "Removing unchanged %s: %s" type_ name;
      removeNode devElement;
      None
    else
      log.Verbose "Adding modified %s: %s" type_ name;
      (Some (callback id callbackKind))
  )

type NodeEntityDecision =
  | AddEntity
  | RemoveEntity
  | NotEntity of XmlNode * XmlNode * RetrieveEntityResponse
  | UnhandledEntity


let diffEntity (diffSolutionInfo: DiffSolutionInfo) genericAddToSolution (dev_ent: XmlNode) (prod_ent: XmlNode) (resp: RetrieveEntityResponse) =
  let { solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  let checkDifference = genericAddToSolution dev_ent prod_ent (Some resp)  
  seq {
    yield! checkDifference |> entityAttributeHandler resp diffSolutionUniqueName
    yield! checkDifference |> entityMetadataHandler
    yield! checkDifference |> entityRibbonHandler
    yield! checkDifference |> entityFormHandler
    yield! checkDifference |> entityViewHandler
    yield! checkDifference |> entityChartHandler 
  }

let decideEntityXmlDifference (diffSolutionInfo: DiffSolutionInfo) (devEntity: XmlNode) (prod_node: XmlNode) genericAddToSolution = 
  let { proxy = proxy; solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  let entityNode = devEntity.SelectSingleNode("EntityInfo/entity")
  if entityNode = null then seq { None } else

  let name = entityNode.Attributes.GetNamedItem("Name").Value
  let prodEntity = prod_node.SelectSingleNode("/ImportExportXml/Entities/Entity[EntityInfo/entity/@Name='"+name+"']")

  if prodEntity = null then
    log.Verbose "Adding new entity: %s" name;
    let resp = fetchEntityAllMetadata proxy name
      
    seq { 
      yield (Some (createSolutionComponentRequest diffSolutionUniqueName resp.EntityMetadata.MetadataId.Value SolutionComponent.Entity))
    }

  elif devEntity.OuterXml = prodEntity.OuterXml then
    log.Verbose "Removing unchanged entity: %s" name;
    removeNode devEntity;
      
    seq { None }
  else
    log.Verbose "Processing entity: %s" name;
    let resp = fetchEntityAllMetadata proxy name
      
    seq { yield! diffEntity diffSolutionInfo genericAddToSolution devEntity prodEntity resp }

// Help: https://bettercrm.blog/2017/04/26/solution-component-types-in-dynamics-365/
let rec diffSolution (diffSolutionInfo: DiffSolutionInfo) (devNode: XmlNode) (prodNode: XmlNode) =
  log.Verbose "Preprocessing";
  let expr = "//IntroducedVersion|//IsDataSourceSecret|//Format|//CanChangeDateTimeBehavior|//LookupStyle|//CascadeRollupView|//Length|//TriggerOnUpdateAttributeList[not(text())]"
  selectNodes devNode expr |> Seq.iter removeNode;
  selectNodes prodNode expr |> Seq.iter removeNode;
  
  let genericAddToSolution = diffElement diffSolutionInfo
  let entities = selectNodes devNode "/ImportExportXml/Entities/Entity"
  
  let { proxy = proxy; solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  
  let batchedDiff = seq {
    for dev_ent in entities do yield! decideEntityXmlDifference diffSolutionInfo dev_ent prodNode genericAddToSolution

    let checkDifference = genericAddToSolution devNode prodNode None

    yield! checkDifference |> solutionRoleHandler
    yield! checkDifference |> solutionWorkflowHandler
    yield! checkDifference |> solutionFieldSecurityProfileHandler
    yield! checkDifference |> solutionEntityRelationshipHandler proxy diffSolutionUniqueName
    yield! checkDifference |> solutionOptionSetHandler proxy diffSolutionUniqueName
    yield! checkDifference |> solutionDashboardHandler
    yield! checkDifference |> solutionWebResourceHandler
    yield! checkDifference |> solutionPluginAssemblyHandler devNode diffSolutionUniqueName
    yield! checkDifference |> solutionPluginStepHandler devNode diffSolutionUniqueName
    yield! checkDifference |> solutionAppHandler proxy diffSolutionUniqueName
    yield! checkDifference |> solutionAppSiteMapHandler proxy diffSolutionUniqueName
  }

  batchedDiff 
  |> Seq.toArray
  |> Array.Parallel.choose (id)
  |> Array.Parallel.map (fun x -> x :> OrganizationRequest)
  |> CrmDataHelper.performAsBulk proxy 

let diff (proxy: IOrganizationService) diffSolutionUniqueName (devExtractedPath: string) (prodExtractedPath: string) =
  log.Verbose "Parsing DEV customizations";
  let devDocument = XmlDocument ()
  devDocument.Load (devExtractedPath + "/customizations.xml");

  log.Verbose "Parsing PROD customizations";
  let prodDocument = XmlDocument ()
  prodDocument.Load (prodExtractedPath + "/customizations.xml");

  log.Verbose "Calculating diff";
  let diffSolutionInfo = {
    proxy = proxy
    devExtractedPath = devExtractedPath
    prodExtractedPath = prodExtractedPath
    solutionUniqueName = diffSolutionUniqueName
  }

  diffSolution diffSolutionInfo devDocument prodDocument
  // log.Verbose "Saving";
  // File.WriteAllText (__SOURCE_DIRECTORY__ + @"\diff.xml", to_string dev_doc);