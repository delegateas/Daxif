module DG.Daxif.Modules.Solution.SolutionDiffHelper


open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Query;
open Microsoft.Xrm.Sdk.Metadata;
open Microsoft.Xrm.Sdk.Messages;
open System.IO
open System
open System.Xml
open DG.Daxif.Common
open Microsoft.Crm.Sdk.Messages
open System.Text.RegularExpressions
open System.Threading
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

  let result = devElements |> Seq.map (fun devElement ->

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
  result

type NodeEntityDecision =
  | AddEntity // in future, yield an add request
  | RemoveEntity // in future, yield remove request
  | NotEntity of XmlNode * XmlNode * RetrieveEntityResponse
  | UnhandledEntity


let diffEntity (diffSolutionInfo: DiffSolutionInfo) genericAddToSolution (dev_ent: XmlNode) (prod_ent: XmlNode) (resp: RetrieveEntityResponse) =
  let { solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  let checkDifference = genericAddToSolution dev_ent prod_ent (Some resp)  
  seq{
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
      
    seq{
      yield! diffEntity diffSolutionInfo genericAddToSolution devEntity prodEntity resp
    }

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

  printf "%A" batchedDiff
  batchedDiff 
  |> Seq.toArray
  |> Array.Parallel.choose (id)
  |> Array.Parallel.map (fun x -> x :> OrganizationRequest)
  |> DG.Daxif.Common.CrmDataHelper.performAsBulk proxy 

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

let export fileLocation completeSolutionName temporarySolutionName (dev:DG.Daxif.Environment) (prod:DG.Daxif.Environment) = 
  log.Info "Starting diff export"
  Directory.CreateDirectory(fileLocation) |> ignore;
  // Export [complete solution] from DEV and PROD
  let ((devProxy, devSolution), (prodProxy, prodSolution)) = 
    [| dev; prod |]
    |> Array.Parallel.map (fun env -> 
      log.Verbose "Connecting to %s" env.name;
      let proxy = env.connect().GetProxy()
      Directory.CreateDirectory(fileLocation + "/" + env.name) |> ignore;

      log.Verbose "Exporting solution '%s' from %s" completeSolutionName env.name;
      let sol = downloadSolutionRetry env (fileLocation + "/" + env.name + "/") completeSolutionName 15 1
      unzip sol;
      (proxy, sol))
    |> function [| devSolution; prodSolution |] -> (devSolution, prodSolution) | _ -> failwith "Impossible"
  
  let publisherId = (fetchSolution devProxy completeSolutionName).Attributes.["publisherid"]

  // Create new [partial solution] on DEV
  let id = createSolution devProxy temporarySolutionName publisherId
  try
    let requestsFromDiff = diff devProxy temporarySolutionName devSolution prodSolution

    // Export [partial solution] from DEV
    log.Verbose "Exporting solution '%s' from %s" temporarySolutionName dev.name;
    let temp_sol = downloadSolution dev (fileLocation + "/") temporarySolutionName 15
    // Delete [partial solution] on DEV
    log.Verbose "Deleting solution '%s'" temporarySolutionName;
    devProxy.Delete("solution", id);
    log.Info "Done exporting diff solution"
    temporarySolutionName
  with e -> 
    // Delete [partial solution] on DEV
    log.Verbose "Deleting solution '%s'" temporarySolutionName;
    devProxy.Delete("solution", id);
    failwith e.Message; 


let import solutionZipPath complete_solution_name temporary_solution_name (env:DG.Daxif.Environment) = 
  log.Verbose "Connecting to environment %s" env.name;
  let proxy = env.connect().GetProxy()
  // TODO: Remove all Daxif plugin steps
  // Import [partial solution] to TARGET
  let fileBytes = File.ReadAllBytes(solutionZipPath + "/" + temporary_solution_name + ".zip")
  let stopWatch = System.Diagnostics.Stopwatch.StartNew()
  
  executeImportRequestWithProgress proxy fileBytes

  let temp = solutionZipPath + "/" + temporary_solution_name
  unzip temp;
  
  log.Verbose "Parsing TEMP customizations";
  let xml = XmlDocument ()
  xml.Load (temp + "/customizations.xml");

  log.Verbose "Setting workflow states";
  setWorkflowStates proxy xml
  
  // Run through solution components of [partial solution], add to [complete solution] on TARGET
  let tempSolution = fetchSolution proxy temporary_solution_name

  log.Info "Publishing changes"
  CrmDataHelper.publishAll proxy

  stopWatch.Stop()
  
  log.Info "Downtime: %.1f minutes" stopWatch.Elapsed.TotalMinutes;
  transferSolutionComponents proxy tempSolution.Id complete_solution_name
  
  // Delete [partial solution] on TARGET
  log.Verbose "Deleting solution '%s'" temporary_solution_name
  proxy.Delete("solution", tempSolution.Id)
    