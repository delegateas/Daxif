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

type output =
  | Silent
  | Same
  | Diff
  | Both

type id_node = 
  | Attribute of string
  | Node of string
  | Custom of (string -> string)

let getId (elem: XmlNode) = function
  | Attribute id ->
    elem.Attributes.GetNamedItem(id).Value
  | Node id ->
    elem.SelectSingleNode(id).InnerText
  | Custom _ ->
    elem.Name

let append_selector path id = function
  | Attribute att ->
    path + "[@"+att+"='"+id+"']"
  | Node att -> 
    path + "["+att+"/text()='"+id+"']"
  | Custom fpath -> 
    fpath id

type ReadableName =
  | Static of string // example: "Ribbon"
  | AttributeNamedItem of string
  | ElementName // elem.name
  | InnerTextDisplayName
  | LocalizedNameDescription

let GetReadableName (elem: XmlNode) = function
  | Static name -> name
  | ElementName -> elem.Name
  | AttributeNamedItem attrName -> elem.Attributes.GetNamedItem(attrName).Value
  | InnerTextDisplayName -> elem.SelectSingleNode("DisplayName").InnerText
  | LocalizedNameDescription -> elem.SelectSingleNode("LocalizedNames/LocalizedName").Attributes.GetNamedItem("description").Value
  
type XmlPath = string

let workflowGuidReplace ((dev_path, prod_path): (string*string)) (dev_elem: XmlNode) (prod_elem: XmlNode) =
  let dev_file = File.ReadAllText(dev_path + dev_elem.SelectSingleNode("XamlFileName").InnerText)
  let prod_file = File.ReadAllText(prod_path + prod_elem.SelectSingleNode("XamlFileName").InnerText)
  let expr = " Version=.+?,|\s*<x:Null x:Key=\"Description\" />|\s*<x:Boolean x:Key=\"ContainsElseBranch\">False</x:Boolean>"
  let dev_file = 
    Regex.Replace(dev_file, 
      "\[New Object\(\) \{ Microsoft\.Xrm\.Sdk\.Workflow\.WorkflowPropertyType\.Guid, \"(........-....-....-....-............)\", \"Key\" \}\]", 
      "[New Object() { Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.Guid, \"$1\", \"UniqueIdentifier\" }]")
  let prod_file = 
    Regex.Replace(prod_file, 
      "\[New Object\(\) \{ Microsoft\.Xrm\.Sdk\.Workflow\.WorkflowPropertyType\.Guid, \"(........-....-....-....-............)\", \"Key\" \}\]", 
      "[New Object() { Microsoft.Xrm.Sdk.Workflow.WorkflowPropertyType.Guid, \"$1\", \"UniqueIdentifier\" }]")
  Regex.Replace(dev_file, expr, "").Length = Regex.Replace(prod_file, expr, "").Length
  // dev_file = prod_file

let webResourceByteDiff ((dev_path, prod_path): (string*string))  (dev_elem: XmlNode) (prod_elem: XmlNode) = 
  let dev_file = File.ReadAllBytes(dev_path + dev_elem.SelectSingleNode("FileName").InnerText)
  let prod_file = File.ReadAllBytes(prod_path + prod_elem.SelectSingleNode("FileName").InnerText)
  dev_file = prod_file

type ExtraChecks =
  | NoExtra 
  | WebResourceByteDiff
  | WorkflowGuidReplace

let extraCheckFun (dev_path, prod_path) (dev_elem: XmlNode) (prod_elem: XmlNode) = function
  | NoExtra -> true
  | WorkflowGuidReplace -> workflowGuidReplace (dev_path, prod_path) dev_elem prod_elem
  | WebResourceByteDiff -> webResourceByteDiff (dev_path, prod_path) dev_elem prod_elem


type AddToSolutionStrategy = 
  | EntityComponentAdd of EntityComponent
  | SolutionComponentAdd of SolutionComponent

type CallbackKind =
  | EntityMetadataCallback
  | RibbonCallback
  | CustomCallback of (string -> AddSolutionComponentRequest)
  | EntityComponentCallback of EntityComponent
  | SolutionComponentCallback of SolutionComponent

let callbackFun proxy diffSolutionUniqueName (resp: RetrieveEntityResponse option) id = function 
  | EntityMetadataCallback -> 
      createEntityComponent proxy diffSolutionUniqueName resp.Value.EntityMetadata.MetadataId.Value EntityComponent.EntityMetaData
  | RibbonCallback ->
      createSolutionComponent proxy diffSolutionUniqueName resp.Value.EntityMetadata.MetadataId.Value SolutionComponent.Ribbon
  | CustomCallback callback -> 
      callback id
  | EntityComponentCallback comp -> 
      createEntityComponent proxy diffSolutionUniqueName (Guid.Parse id) comp
  | SolutionComponentCallback comp-> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) comp

type DiffSolutionInfo = {
  proxy: IOrganizationService
  devExtractedPath: string
  prodExtractedPath: string
  solutionUniqueName: string
}

let diffElement (diffSolutionInfo: DiffSolutionInfo) output (devNode: XmlNode) (prodNode: XmlNode) (resp: RetrieveEntityResponse option)
              type_ devNodePath devId (devReadable: ReadableName) (extraCheck: ExtraChecks) (callbackKind: CallbackKind) =
  let { proxy = proxy; solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  let devElements = selectNodes devNode devNodePath
  let result = devElements |> Seq.map (fun devElement ->
    let id = getId devElement devId
    let name = GetReadableName devElement devReadable
    let prodElement = prodNode.SelectSingleNode(append_selector devNodePath id devId)
    let extraCheckfunction = extraCheckFun (diffSolutionInfo.devExtractedPath, diffSolutionInfo.prodExtractedPath) 
                                            devElement prodElement extraCheck
    
    let callback = callbackFun proxy diffSolutionUniqueName resp
    // remove_useless dev_elem prod_elem;
    if prodElement = null then
      if output = Diff || output = Both then 
        log.Verbose "Adding new %s: %s" type_ name;
      (Some (callback id callbackKind))
    else if devElement.OuterXml = prodElement.OuterXml && extraCheckfunction then
      if output = Same || output = Both then 
        log.Verbose "Removing unchanged %s: %s" type_ name;
      removeNode devElement;
      None
    else
      if output = Diff || output = Both then 
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
  let { proxy = proxy; solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  let AddEntityDataToSolution = genericAddToSolution dev_ent prod_ent (Some resp)  
  seq{
    yield! AddEntityDataToSolution "entity info" "EntityInfo/entity/*[not(self::attributes)]" 
      (Custom (fun id -> "EntityInfo/entity/"+id))
      ElementName
      NoExtra
      EntityMetadataCallback
  
    yield! AddEntityDataToSolution "ribbon" "RibbonDiffXml"
      (Custom (fun id -> "RibbonDiffXml"))
      (Static "Ribbon")
      NoExtra
      RibbonCallback
  
    yield! AddEntityDataToSolution "attribute" "EntityInfo/entity/attributes/attribute"
      (Attribute "PhysicalName")
      (AttributeNamedItem "PhysicalName")
      NoExtra
      (CustomCallback (fun id -> 
          resp.EntityMetadata.Attributes
          |> Array.find (fun a -> a.SchemaName = id)
          |> fun a -> createEntityComponent proxy diffSolutionUniqueName a.MetadataId.Value EntityComponent.Attribute)
      )

    yield! AddEntityDataToSolution "form" "FormXml/forms/systemform"
      (Node "formid")
      LocalizedNameDescription
      NoExtra
      (EntityComponentCallback EntityComponent.Form)
  
    yield! AddEntityDataToSolution "view" "SavedQueries/savedqueries/savedquery"
      (Node "savedqueryid")
      LocalizedNameDescription
      NoExtra
      (EntityComponentCallback EntityComponent.View)
  
    yield! AddEntityDataToSolution "chart" "Visualizations/visualization"
      (Node "savedqueryvisualizationid")
      LocalizedNameDescription
      NoExtra
      (EntityComponentCallback EntityComponent.Chart)
  }

let decideEntityXmlDifference (diffSolutionInfo: DiffSolutionInfo) output (devEntity: XmlNode) (prod_node: XmlNode) genericAddToSolution = 
  let { proxy = proxy; solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  let entityNode = devEntity.SelectSingleNode("EntityInfo/entity")
  if entityNode = null then seq { None } else

  let name = entityNode.Attributes.GetNamedItem("Name").Value
  let prodEntity = prod_node.SelectSingleNode("/ImportExportXml/Entities/Entity[EntityInfo/entity/@Name='"+name+"']")

  if prodEntity = null then
    if output = Diff || output = Both then 
      log.Verbose "Adding new entity: %s" name;
    let req = RetrieveEntityRequest ()
    req.EntityFilters <- EntityFilters.All;
    req.LogicalName <- name.ToLower();
    let resp = proxy.Execute(req) :?> RetrieveEntityResponse
      
    seq {
      yield (Some (createSolutionComponent proxy diffSolutionUniqueName resp.EntityMetadata.MetadataId.Value SolutionComponent.Entity))
    }

  elif devEntity.OuterXml = prodEntity.OuterXml then
    if output = Same || output = Both then 
      log.Verbose "Removing unchanged entity: %s" name;
    removeNode devEntity;
      
    seq { None }
  else
    log.Verbose "Processing entity: %s" name;
    let req = RetrieveEntityRequest ()
    req.EntityFilters <- EntityFilters.All;
    req.LogicalName <- name.ToLower();
    let resp = diffSolutionInfo.proxy.Execute(req) :?> RetrieveEntityResponse
      
    seq{
      yield! diffEntity diffSolutionInfo genericAddToSolution devEntity prodEntity resp
    }

// Help: https://bettercrm.blog/2017/04/26/solution-component-types-in-dynamics-365/
let rec diffSolution (diffSolutionInfo: DiffSolutionInfo) (devNode: XmlNode) (prodNode: XmlNode) output =
  log.Verbose "Preprocessing";
  let expr = "//IntroducedVersion|//IsDataSourceSecret|//Format|//CanChangeDateTimeBehavior|//LookupStyle|//CascadeRollupView|//Length|//TriggerOnUpdateAttributeList[not(text())]"
  selectNodes devNode expr |> Seq.iter removeNode;
  selectNodes prodNode expr |> Seq.iter removeNode;
  
  let genericAddToSolution = diffElement diffSolutionInfo output
  let entities = selectNodes devNode "/ImportExportXml/Entities/Entity"
  
  let { proxy = proxy; solutionUniqueName = diffSolutionUniqueName; } = diffSolutionInfo
  
  let batchedDiff = seq {
    for dev_ent in entities do yield! (decideEntityXmlDifference diffSolutionInfo output dev_ent prodNode genericAddToSolution)

    let AddSolutionDataToSolution = genericAddToSolution devNode prodNode None

    yield! AddSolutionDataToSolution "role" "/ImportExportXml/Roles/Role"
      (Attribute "id")
      (AttributeNamedItem "name")
      NoExtra
      (SolutionComponentCallback SolutionComponent.Role)

    yield! AddSolutionDataToSolution "workflow" "/ImportExportXml/Workflows/Workflow"
      (Attribute "WorkflowId")
      (AttributeNamedItem "Name")
      WorkflowGuidReplace
      (SolutionComponentCallback SolutionComponent.Workflow)

    yield! AddSolutionDataToSolution "field security profile" "/ImportExportXml/FieldSecurityProfiles/FieldSecurityProfile"
      (Attribute "fieldsecurityprofileid")
      (AttributeNamedItem "name")
      NoExtra
      (SolutionComponentCallback SolutionComponent.FieldSecurityProfile)
    
    yield! AddSolutionDataToSolution "entity relationships" "/ImportExportXml/EntityRelationships/EntityRelationship"
      (Attribute "Name")
      (AttributeNamedItem "Name")
      NoExtra
      (CustomCallback (fun id -> 
        let req = RetrieveRelationshipRequest ()
        req.Name <- id;
        let resp = proxy.Execute(req) :?> RetrieveRelationshipResponse
        createSolutionComponent proxy diffSolutionUniqueName resp.RelationshipMetadata.MetadataId.Value SolutionComponent.EntityRelationship);
      )

    yield! AddSolutionDataToSolution "option set" "/ImportExportXml/optionsets/optionset"
      (Attribute "Name")
      (AttributeNamedItem "localizedName")
      NoExtra
      (CustomCallback (fun id -> 
          let req = RetrieveOptionSetRequest ()
          req.Name <- id;
          let resp = proxy.Execute(req) :?> RetrieveOptionSetResponse
          createSolutionComponent proxy diffSolutionUniqueName resp.OptionSetMetadata.MetadataId.Value SolutionComponent.OptionSet);
       )
  
    yield! AddSolutionDataToSolution "dashboard" "/ImportExportXml/Dashboards/Dashboard"
      (Node "FormId")
      LocalizedNameDescription
      NoExtra
      (SolutionComponentCallback SolutionComponent.Dashboard)

    yield! AddSolutionDataToSolution "web resource" "/ImportExportXml/WebResources/WebResource"
      (Node "WebResourceId")
      InnerTextDisplayName
      WebResourceByteDiff
      (SolutionComponentCallback SolutionComponent.WebResource)
    
    yield! addAll "plugin assembly" output
      devNode
      "/ImportExportXml/SolutionPluginAssemblies/PluginAssembly"
      (fun elem -> elem.Attributes.GetNamedItem("PluginAssemblyId").Value)
      (fun elem -> elem.Attributes.GetNamedItem("FullName").Value)
      (fun dev_elem -> true)
      (fun id -> 
        (Some (createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.PluginAssembly)))

    yield! addAll "plugin step" output
      devNode
      "/ImportExportXml/SdkMessageProcessingSteps/SdkMessageProcessingStep"
      (fun elem -> elem.Attributes.GetNamedItem("SdkMessageProcessingStepId").Value)
      (fun elem -> elem.Attributes.GetNamedItem("Name").Value)
      (fun dev_elem -> true)
      (fun id -> 
        (Some (createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.PluginStep)))

    // (Reports)
    // (regular Sitemap)
    yield! AddSolutionDataToSolution "app site map" "/ImportExportXml/AppModuleSiteMaps/AppModuleSiteMap"
      (Node "SiteMapUniqueName")
      LocalizedNameDescription
      NoExtra
      (CustomCallback (fun id -> 
        createSolutionComponent proxy diffSolutionUniqueName (fetchSitemapId proxy id) SolutionComponent.SiteMap))

    yield! AddSolutionDataToSolution "app" "/ImportExportXml/AppModules/AppModule"
      (Node "UniqueName")
      LocalizedNameDescription
      NoExtra
      (CustomCallback (fun id -> 
        createSolutionComponent proxy diffSolutionUniqueName (fetchAppModuleId proxy id) SolutionComponent.AppModule))
  }

  printf "%A" batchedDiff
  batchedDiff 
  |> Seq.toArray
  |> Array.Parallel.choose (id)
  |> Array.Parallel.map (fun x -> x :> OrganizationRequest)
  |> DG.Daxif.Common.CrmDataHelper.performAsBulk proxy 
    
let xmlToString (doc : XmlDocument) =
  let ws = new XmlWriterSettings()
  ws.OmitXmlDeclaration <- true;
  let sw = new StringWriter ()
  let writer = XmlWriter.Create(sw, ws)
  doc.Save(writer);
  sw.ToString()

let diff (proxy: IOrganizationService) diffSolutionUniqueName (devExtractedPath: string) (prodExtractedPath: string) output =
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

  diffSolution diffSolutionInfo devDocument prodDocument output |> ignore;
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
  // Get publisher from [complete solution]
  let completeSolution = fetchSolution devProxy completeSolutionName
  // Create new [partial solution] on DEV
  let id = createSolution devProxy temporarySolutionName completeSolution.Attributes.["publisherid"]
  try
    // Diff the two exported solutions, add to [partial solution]
    diff devProxy temporarySolutionName devSolution prodSolution Diff;
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

let rec fetchImportStatus proxy id asyncid =
  Thread.Sleep 10000;
  match fetchImportStatusOnce proxy id with
    | None -> 
      log.Verbose "Import not started yet.";
      fetchImportStatus proxy id asyncid
    | Some (_, _, data) when data.Contains("<result result=\"failure\"") -> 
      let job = CrmData.CRUD.retrieve proxy "asyncoperation" asyncid
      if job.Attributes.ContainsKey "message" then
        log.Verbose "%s" (job.Attributes.["message"] :?> string);
      else 
        let doc = XmlDocument ()
        doc.LoadXml data;
        selectNodes doc "//result[@result='failure']"
        |> Seq.iter (fun a -> log.Verbose "%s" (a.Attributes.GetNamedItem "errortext").Value)
      failwithf "An error occured in import.";
    | Some (true, _, _) -> 
      log.Info "Import succesful.";
    | Some (false, progress, _) -> 
      log.Verbose "Importing: %.1f%%" progress;
      fetchImportStatus proxy id asyncid

let import solutionZipPath complete_solution_name temporary_solution_name (env:DG.Daxif.Environment) = 
  log.Verbose "Connecting to environment %s" env.name;
  let proxy = env.connect().GetProxy()
  // TODO: Remove all Daxif plugin steps
  // Import [partial solution] to TARGET
  let fileBytes = File.ReadAllBytes(solutionZipPath + "/" + temporary_solution_name + ".zip")
  let stopWatch = System.Diagnostics.Stopwatch.StartNew()
  log.Info "Importing solution";
  let importid = Guid.NewGuid()
  let req = ImportSolutionRequest ()
  req.CustomizationFile <- fileBytes;
  req.ImportJobId <- importid;
  let async_req = ExecuteAsyncRequest ()
  async_req.Request <- req;
  let resp = proxy.Execute(async_req) :?> ExecuteAsyncResponse
  fetchImportStatus proxy importid resp.AsyncJobId;
  let temp = solutionZipPath + "/" + temporary_solution_name
  unzip temp;
  
  log.Verbose "Parsing TEMP customizations";
  let xml = XmlDocument ()
  xml.Load (temp + "/customizations.xml");

  log.Verbose "Setting workflow states";
  let workflows = selectNodes xml "/ImportExportXml/Workflows/Workflow"
  workflows
  |> Seq.iter (fun e ->
    let id = e.Attributes.GetNamedItem("WorkflowId").Value
    let state = int (e.SelectSingleNode("StateCode").InnerText)
    let status = int (e.SelectSingleNode("StatusCode").InnerText)
    let req = 
      SetStateRequest(
        State = new OptionSetValue(state), 
        Status = new OptionSetValue(status), 
        EntityMoniker = new EntityReference("workflow", Guid.Parse(id)))
    proxy.Execute(req) |> ignore
    )
  // Run through solution components of [partial solution], add to [complete solution] on TARGET
  let sol = fetchSolution proxy temporary_solution_name

  log.Info "Publishing changes"
  CrmDataHelper.publishAll proxy

  stopWatch.Stop()
  
  log.Info "Downtime: %.1f minutes" stopWatch.Elapsed.TotalMinutes;
  transferSolutionComponents proxy sol.Id complete_solution_name
  
  // Delete [partial solution] on TARGET
  log.Verbose "Deleting solution '%s'" temporary_solution_name
  proxy.Delete("solution", sol.Id)
    