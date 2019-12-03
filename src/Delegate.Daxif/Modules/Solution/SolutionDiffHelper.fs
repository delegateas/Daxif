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
  | ElemName // elem.name
  | InnerTextDisplayName
  | LocalizedNameDescription

let GetReadableName (elem: XmlNode) = function
  | Static name -> name
  | ElemName -> elem.Name
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

let GetExtraCheck (dev_path, prod_path) (dev_elem: XmlNode) (prod_elem: XmlNode) = function
  | NoExtra -> true
  | WorkflowGuidReplace -> workflowGuidReplace (dev_path, prod_path) dev_elem prod_elem
  | WebResourceByteDiff -> webResourceByteDiff (dev_path, prod_path) dev_elem prod_elem


type AddToSolutionStrategy = 
  | EntityComponentAdd of EntityComponent
  | SolutionComponentAdd of SolutionComponent

let elim_elem ((devExtractedPath, prodExtractedPath): (string * string)) output (devNode: XmlNode) (prodNode: XmlNode) 
              type_ devNodePath devId (devReadable: ReadableName) (extraCheck: ExtraChecks) callback =
  let devElements = selectNodes devNode devNodePath
  devElements
  |> Seq.iter (fun devElement ->
    let id = getId devElement devId
    let name = GetReadableName devElement devReadable
    let prodElement = prodNode.SelectSingleNode(append_selector devNodePath id devId)
    let extraCheckfunction = GetExtraCheck (devExtractedPath, prodExtractedPath) devElement prodElement extraCheck
    // remove_useless dev_elem prod_elem;
    if prodElement = null then
      if output = Diff || output = Both then 
        log.Verbose "Adding new %s: %s" type_ name;
      callback id;
    else if devElement.OuterXml = prodElement.OuterXml && extraCheckfunction then
      if output = Same || output = Both then 
        log.Verbose "Removing unchanged %s: %s" type_ name;
      removeNode devElement;
    else
      if output = Diff || output = Both then 
        log.Verbose "Adding modified %s: %s" type_ name;
      callback id;
    )

type NodeEntityDecision =
  | AddEntity // in future, yield an add request
  | RemoveEntity // in future, yield remove request
  | NotEntity of XmlNode * XmlNode * RetrieveEntityResponse
  | UnhandledEntity

let NodeIsNotEntity (proxy: IOrganizationService) diffSolutionUniqueName output (devEntity: XmlNode) (prod_node: XmlNode) = 
  let entityNode = devEntity.SelectSingleNode("EntityInfo/entity")
  if entityNode <> null then
    let name = entityNode.Attributes.GetNamedItem("Name").Value
    let prodEntity = prod_node.SelectSingleNode("/ImportExportXml/Entities/Entity[EntityInfo/entity/@Name='"+name+"']")
    if prodEntity = null then
      if output = Diff || output = Both then 
        log.Verbose "Adding new entity: %s" name;
      let req = RetrieveEntityRequest ()
      req.EntityFilters <- EntityFilters.All;
      req.LogicalName <- name.ToLower();
      let resp = proxy.Execute(req) :?> RetrieveEntityResponse
      createSolutionComponent proxy diffSolutionUniqueName resp.EntityMetadata.MetadataId.Value SolutionComponent.Entity
      AddEntity
    elif devEntity.OuterXml = prodEntity.OuterXml then
      if output = Same || output = Both then 
        log.Verbose "Removing unchanged entity: %s" name;
      removeNode devEntity;
      RemoveEntity
    else
      log.Verbose "Processing entity: %s" name;
      let req = RetrieveEntityRequest ()
      req.EntityFilters <- EntityFilters.All;
      req.LogicalName <- name.ToLower();
      let resp = proxy.Execute(req) :?> RetrieveEntityResponse
      NotEntity (devEntity, prodEntity, resp)
   else
     UnhandledEntity

// Help: https://bettercrm.blog/2017/04/26/solution-component-types-in-dynamics-365/
let rec elim (proxy: IOrganizationService) diffSolutionUniqueName (devCustomizations: string) (prodCustomizations: string) (devNode: XmlNode) (prodNode: XmlNode) output =
  log.Verbose "Preprocessing";
  let expr = "//IntroducedVersion|//IsDataSourceSecret|//Format|//CanChangeDateTimeBehavior|//LookupStyle|//CascadeRollupView|//Length|//TriggerOnUpdateAttributeList[not(text())]"
  selectNodes devNode expr |> Seq.iter removeNode;
  selectNodes prodNode expr |> Seq.iter removeNode;
  
  let GenericAddToSolution = elim_elem (devCustomizations, prodCustomizations) output
  let entities = selectNodes devNode "/ImportExportXml/Entities/Entity"
  
  entities
  |> Seq.iter (fun dev_ent -> 
    let isEntityNode = NodeIsNotEntity proxy diffSolutionUniqueName output dev_ent prodNode
    match isEntityNode with 
    | NotEntity (dev_ent, prod_ent, resp) ->
      let AddEntityDataToSolution = GenericAddToSolution dev_ent prod_ent
      
      AddEntityDataToSolution "entity info" "EntityInfo/entity/*[not(self::attributes)]" 
        (Custom (fun id -> "EntityInfo/entity/"+id))
        ElemName
        NoExtra
        (fun id -> 
          createEntityComponent proxy diffSolutionUniqueName resp.EntityMetadata.MetadataId.Value EntityComponent.EntityMetaData);
      
      AddEntityDataToSolution "ribbon" "RibbonDiffXml"
        (Custom (fun id -> "RibbonDiffXml"))
        (Static "Ribbon")
        NoExtra
        (fun id -> 
          createSolutionComponent proxy diffSolutionUniqueName resp.EntityMetadata.MetadataId.Value SolutionComponent.Ribbon);
      
      AddEntityDataToSolution "attribute" "EntityInfo/entity/attributes/attribute"
        (Attribute "PhysicalName")
        (AttributeNamedItem "PhysicalName")
        NoExtra
        (fun id -> 
          resp.EntityMetadata.Attributes
          |> Array.find (fun a -> a.SchemaName = id)
          |> fun a -> createEntityComponent proxy diffSolutionUniqueName a.MetadataId.Value EntityComponent.Attribute);
      AddEntityDataToSolution "form" "FormXml/forms/systemform"
        (Node "formid")
        LocalizedNameDescription
        NoExtra
        (fun id -> 
          createEntityComponent proxy diffSolutionUniqueName (Guid.Parse id) EntityComponent.Form);
      
      AddEntityDataToSolution "view" "SavedQueries/savedqueries/savedquery"
        (Node "savedqueryid")
        LocalizedNameDescription
        NoExtra
        (fun id -> 
          createEntityComponent proxy diffSolutionUniqueName (Guid.Parse id) EntityComponent.View);
      
      AddEntityDataToSolution "chart" "Visualizations/visualization"
        (Node "savedqueryvisualizationid")
        LocalizedNameDescription
        NoExtra
        (fun id -> 
          createEntityComponent proxy diffSolutionUniqueName (Guid.Parse id) EntityComponent.Chart);
      | _ -> ()
    )
  let AddSolutionDataToSolution = elim_elem (devCustomizations, prodCustomizations) output devNode prodNode

  AddSolutionDataToSolution "role" "/ImportExportXml/Roles/Role"
    (Attribute "id")
    (AttributeNamedItem "name")
    NoExtra
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.Role);
  AddSolutionDataToSolution "workflow" "/ImportExportXml/Workflows/Workflow"
    (Attribute "WorkflowId")
    (AttributeNamedItem "Name")
    WorkflowGuidReplace
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.Workflow);
  AddSolutionDataToSolution "field security profile" "/ImportExportXml/FieldSecurityProfiles/FieldSecurityProfile"
    (Attribute "fieldsecurityprofileid")
    (AttributeNamedItem "name")
    NoExtra
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.FieldSecurityProfile);
  AddSolutionDataToSolution "entity relationships" "/ImportExportXml/EntityRelationships/EntityRelationship"
    (Attribute "Name")
    (AttributeNamedItem "Name")
    NoExtra
    (fun id -> 
      let req = RetrieveRelationshipRequest ()
      req.Name <- id;
      let resp = proxy.Execute(req) :?> RetrieveRelationshipResponse
      createSolutionComponent proxy diffSolutionUniqueName resp.RelationshipMetadata.MetadataId.Value SolutionComponent.EntityRelationship);
  AddSolutionDataToSolution "option set" "/ImportExportXml/optionsets/optionset"
    (Attribute "Name")
    (AttributeNamedItem "localizedName")
    NoExtra
    (fun id -> 
      let req = RetrieveOptionSetRequest ()
      req.Name <- id;
      let resp = proxy.Execute(req) :?> RetrieveOptionSetResponse
      createSolutionComponent proxy diffSolutionUniqueName resp.OptionSetMetadata.MetadataId.Value SolutionComponent.OptionSet);
  AddSolutionDataToSolution "dashboard" "/ImportExportXml/Dashboards/Dashboard"
    (Node "FormId")
    LocalizedNameDescription
    NoExtra
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.Dashboard);
  AddSolutionDataToSolution "web resource" "/ImportExportXml/WebResources/WebResource"
    (Node "WebResourceId")
    InnerTextDisplayName
    WebResourceByteDiff
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.WebResource);
  addAll "plugin assembly" output
    devNode
    "/ImportExportXml/SolutionPluginAssemblies/PluginAssembly"
    (fun elem -> elem.Attributes.GetNamedItem("PluginAssemblyId").Value)
    (fun elem -> elem.Attributes.GetNamedItem("FullName").Value)
    (fun dev_elem -> true)
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.PluginAssembly);
  addAll "plugin step" output
    devNode
    "/ImportExportXml/SdkMessageProcessingSteps/SdkMessageProcessingStep"
    (fun elem -> elem.Attributes.GetNamedItem("SdkMessageProcessingStepId").Value)
    (fun elem -> elem.Attributes.GetNamedItem("Name").Value)
    (fun dev_elem -> true)
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (Guid.Parse id) SolutionComponent.PluginStep);
  // (Reports)
  // (regular Sitemap)
  AddSolutionDataToSolution "app site map" "/ImportExportXml/AppModuleSiteMaps/AppModuleSiteMap"
    (Node "SiteMapUniqueName")
    LocalizedNameDescription
    NoExtra
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (fetchSitemapId proxy id) SolutionComponent.SiteMap);
  AddSolutionDataToSolution "app" "/ImportExportXml/AppModules/AppModule"
    (Node "UniqueName")
    LocalizedNameDescription
    NoExtra
    (fun id -> 
      createSolutionComponent proxy diffSolutionUniqueName (fetchAppModuleId proxy id) SolutionComponent.AppModule);
    
let to_string (doc : XmlDocument) =
  let ws = new XmlWriterSettings()
  ws.OmitXmlDeclaration <- true;
  let sw = new StringWriter ()
  let writer = XmlWriter.Create(sw, ws)
  doc.Save(writer);
  sw.ToString()

let diff (proxy: IOrganizationService) diffSolutionUniqueName (devCustomizations: string) (prodCustomizations: string) output =
  log.Verbose "Parsing DEV customizations";
  let devDocument = XmlDocument ()
  devDocument.Load (devCustomizations + "/customizations.xml");
  log.Verbose "Parsing PROD customizations";
  let prodDocument = XmlDocument ()
  prodDocument.Load (prodCustomizations + "/customizations.xml");
  log.Verbose "Calculating diff";
  elim proxy diffSolutionUniqueName devCustomizations prodCustomizations devDocument prodDocument output |> ignore;
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
    