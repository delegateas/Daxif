module DG.Daxif.Modules.Solution.SolutionDiffHelper


open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Query;
open Microsoft.Xrm.Sdk.Metadata;
open Microsoft.Xrm.Sdk.Messages;
open System.IO
open System
open Microsoft.Xrm.Sdk.Client
open System.Xml
open DG.Daxif.Common
open Microsoft.Crm.Sdk.Messages
open System.IO.Compression
open System.Text.RegularExpressions
open System.Threading
open DG.Daxif.Modules.Solution
open InternalUtility

let rec assoc_right_opt key map = 
  match map with
    | [] -> None
    | (v, k) :: map' ->
      if k = key
      then Some v
      else assoc_right_opt key map'

let assoc_right key map = 
  (assoc_right_opt key map).Value

let get_global_option_set (proxy: IOrganizationService) name =
  let req = RetrieveOptionSetRequest ()
  req.Name <- name;
  let resp = proxy.Execute(req) :?> RetrieveOptionSetResponse
  let oSetMeta = resp.OptionSetMetadata :?> OptionSetMetadata
  oSetMeta.Options
  |> Seq.map (fun o -> (o.Label.UserLocalizedLabel.Label, o.Value.Value))
  |> Seq.toList

let get_component_types proxy = get_global_option_set proxy "componenttype"

let fetch_sitemap_id (proxy: IOrganizationService) (unique_name: string) = 
  let query = QueryExpression("sitemap")
  query.ColumnSet <- ColumnSet(false);
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("sitemapnameunique", ConditionOperator.Equal, unique_name);
  CrmDataHelper.retrieveMultiple proxy query
  |> Seq.head
  |> fun a -> a.Id

let fetch_app_module_id (proxy: IOrganizationService) (unique_name: string) = 
  let query = QueryExpression("appmodule")
  query.ColumnSet <- ColumnSet(false);
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, unique_name);
  CrmDataHelper.retrieveMultiple proxy query
  |> Seq.head
  |> fun a -> a.Id

let download (env: DG.Daxif.Environment) file_location sol_name minutes =
  let usr, pwd, domain = env.getCreds()
  let ac = CrmAuth.getCredentials env.apToUse usr pwd domain
  let ac' = CrmAuth.getCredentials env.apToUse usr pwd domain

  log.Verbose "Exporting extended solution %A" (file_location + sol_name)
  SolutionHelper.exportWithExtendedSolution' env.url ac ac' sol_name file_location false (DG.Daxif.ConsoleLogger DG.Daxif.LogLevel.Verbose)
  file_location + sol_name

let rec download_with_retry (env: DG.Daxif.Environment) file_location sol_name minutes retry_count =
  if retry_count > 0 then
    try
      download env file_location sol_name minutes
    with _ ->
      download_with_retry env file_location sol_name minutes retry_count
  else
    download env file_location sol_name minutes

let unzip file =
  log.Verbose "Unpacking zip '%s' to '%s'" (file + ".zip") file;
  if Directory.Exists(file) then
    Directory.Delete(file, true) |> ignore;
  ZipFile.ExtractToDirectory(file + ".zip", file);

let fetch_solution proxy (solution: string) = 
  let columnSet = ColumnSet("uniquename", "friendlyname", "publisherid", "version")
  CrmDataInternal.Entities.retrieveSolution proxy solution columnSet

let create_solution (proxy: IOrganizationService) temporary_solution_name publisher = 
  log.Verbose "Creating solution '%s'" temporary_solution_name;
  let upd = Entity ("solution")
  upd.Attributes.["uniquename"] <- temporary_solution_name;
  upd.Attributes.["friendlyname"] <- "Temporary. For deploy";
  upd.Attributes.["publisherid"] <- publisher;
  proxy.Create upd

type EntityComponent = 
  | EntityMetaData = 1
  | Attribute = 2
  | View = 26
  | Chart = 59
  | Form = 60

let add_entity_component (proxy: IOrganizationService) sol_id comp_id (comp_type: EntityComponent) =
  let compTypeId = LanguagePrimitives.EnumToValue comp_type
  let req = 
    AddSolutionComponentRequest (
      AddRequiredComponents = false,
      ComponentId = comp_id,
      ComponentType = compTypeId,
      DoNotIncludeSubcomponents = true,
      SolutionUniqueName = sol_id
    )
  proxy.Execute(req) |> ignore

type SolutionComponent = 
  | Entity = 1
  | Ribbon = 1
  | OptionSet = 9
  | EntityRelationship = 10
  | Role = 20
  | Workflow = 29
  | Dashboard = 60 // Dashboard has the same id as form!
  | WebResource = 61
  | SiteMap = 62
  | FieldSecurityProfile = 70
  | AppModule = 80
  | PluginAssembly = 91
  | PluginStep = 92

let add_solution_component (proxy: IOrganizationService) sol_id comp_id (comp_type: SolutionComponent) =
  let compTypeId = LanguagePrimitives.EnumToValue comp_type
  let req = 
    AddSolutionComponentRequest (
      AddRequiredComponents = false,
      ComponentId = comp_id,
      ComponentType = compTypeId,
      SolutionUniqueName = sol_id
    )
  proxy.Execute(req) |> ignore

let select_nodes (node: XmlNode) (xpath: string) =
  node.SelectNodes(xpath)
  |> Seq.cast<XmlNode>

let remove_node (node: XmlNode) =
  if node <> null then
    node.ParentNode.RemoveChild node |> ignore

type output =
  | Silent
  | Same
  | Diff
  | Both
    
let add_all type_ output (dev_node: XmlNode) dev_path dev_id dev_readable extra_check callback =
  let dev_elems = select_nodes dev_node dev_path
  dev_elems
  |> Seq.iter (fun dev_elem ->
    let id = dev_id dev_elem
    let name = dev_readable dev_elem
    log.Verbose "Adding %s: %s" type_ name;
    callback id;
    )

type id_node = 
  | Attribute of string
  | Node of string
  | Custom of (string -> string)

let get_id (elem: XmlNode) = function
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


let elim_elem type_ output (dev_node: XmlNode) (prod_node: XmlNode) dev_path dev_id (dev_readable: ReadableName) extra_check callback =
  let dev_elems = select_nodes dev_node dev_path
  dev_elems
  |> Seq.iter (fun dev_elem ->
    let id = get_id dev_elem dev_id
    let name = GetReadableName dev_elem dev_readable
    let prod_elem = prod_node.SelectSingleNode(append_selector dev_path id dev_id)
    // remove_useless dev_elem prod_elem;
    if prod_elem = null then
      if output = Diff || output = Both then 
        log.Verbose "Adding new %s: %s" type_ name;
      callback id;
    else if dev_elem.OuterXml = prod_elem.OuterXml && extra_check dev_elem prod_elem then
      if output = Same || output = Both then 
        log.Verbose "Removing unchanged %s: %s" type_ name;
      remove_node dev_elem;
    else
      if output = Diff || output = Both then 
        log.Verbose "Adding modified %s: %s" type_ name;
      callback id;
    )

// Help: https://bettercrm.blog/2017/04/26/solution-component-types-in-dynamics-365/
let rec elim (proxy: IOrganizationService) sol_id (dev_customizations: string) (prod_customizations: string) (dev_node: XmlNode) (prod_node: XmlNode) output =
  log.Verbose "Preprocessing";
  let expr = "//IntroducedVersion|//IsDataSourceSecret|//Format|//CanChangeDateTimeBehavior|//LookupStyle|//CascadeRollupView|//Length|//TriggerOnUpdateAttributeList[not(text())]"
  select_nodes dev_node expr |> Seq.iter remove_node;
  select_nodes prod_node expr |> Seq.iter remove_node;

  let entities = select_nodes dev_node "/ImportExportXml/Entities/Entity"
  entities
  |> Seq.iter (fun dev_ent -> 
    let entity_node = dev_ent.SelectSingleNode("EntityInfo/entity")
    if entity_node <> null then
      let name = entity_node.Attributes.GetNamedItem("Name").Value
      let prod_ent = prod_node.SelectSingleNode("/ImportExportXml/Entities/Entity[EntityInfo/entity/@Name='"+name+"']")
      if prod_ent = null then
        if output = Diff || output = Both then 
          log.Verbose "Adding new entity: %s" name;
        let req = RetrieveEntityRequest ()
        req.EntityFilters <- EntityFilters.All;
        req.LogicalName <- name.ToLower();
        let resp = proxy.Execute(req) :?> RetrieveEntityResponse
        add_solution_component proxy sol_id resp.EntityMetadata.MetadataId.Value SolutionComponent.Entity
      else if dev_ent.OuterXml = prod_ent.OuterXml then
        if output = Same || output = Both then 
          log.Verbose "Removing unchanged entity: %s" name;
        remove_node dev_ent;
      else
        log.Verbose "Processing entity: %s" name;
        let req = RetrieveEntityRequest ()
        req.EntityFilters <- EntityFilters.All;
        req.LogicalName <- name.ToLower();
        let resp = proxy.Execute(req) :?> RetrieveEntityResponse
        elim_elem "entity info" output
          dev_ent prod_ent 
          "EntityInfo/entity/*[not(self::attributes)]" 
          (Custom (fun id -> "EntityInfo/entity/"+id))
          ElemName
          (fun dev_elem prod_elem -> true)
          (fun id -> 
            add_entity_component proxy sol_id resp.EntityMetadata.MetadataId.Value EntityComponent.EntityMetaData);
        elim_elem "ribbon" output
          dev_ent prod_ent 
          "RibbonDiffXml"
          (Custom (fun id -> "RibbonDiffXml"))
          (Static "Ribbon")
          (fun dev_elem prod_elem -> true)
          (fun id -> 
            add_solution_component proxy sol_id resp.EntityMetadata.MetadataId.Value SolutionComponent.Ribbon);
        elim_elem "attribute" output
          dev_ent prod_ent 
          "EntityInfo/entity/attributes/attribute"
          (Attribute "PhysicalName")
          (AttributeNamedItem "PhysicalName")
          (fun dev_elem prod_elem -> true)
          (fun id -> 
            resp.EntityMetadata.Attributes
            |> Array.find (fun a -> a.SchemaName = id)
            |> fun a -> add_entity_component proxy sol_id a.MetadataId.Value EntityComponent.Attribute);
        elim_elem "form" output
          dev_ent prod_ent 
          "FormXml/forms/systemform"
          (Node "formid")
          LocalizedNameDescription
          (fun dev_elem prod_elem -> true)
          (fun id -> 
            add_entity_component proxy sol_id (Guid.Parse id) EntityComponent.Form);
        elim_elem "view" output
          dev_ent prod_ent 
          "SavedQueries/savedqueries/savedquery"
          (Node "savedqueryid")
          LocalizedNameDescription
          (fun dev_elem prod_elem -> true)
          (fun id -> 
            add_entity_component proxy sol_id (Guid.Parse id) EntityComponent.View);
        elim_elem "chart" output
          dev_ent prod_ent 
          "Visualizations/visualization"
          (Node "savedqueryvisualizationid")
          LocalizedNameDescription
          (fun dev_elem prod_elem -> true)
          (fun id -> 
            add_entity_component proxy sol_id (Guid.Parse id) EntityComponent.Chart);
    )
  elim_elem "role" output
    dev_node prod_node 
    "/ImportExportXml/Roles/Role"
    (Attribute "id")
    (AttributeNamedItem "name")
    (fun dev_elem prod_elem -> true)
    (fun id -> 
      add_solution_component proxy sol_id (Guid.Parse id) SolutionComponent.Role);
  elim_elem "workflow" output
    dev_node prod_node 
    "/ImportExportXml/Workflows/Workflow"
    (Attribute "WorkflowId")
    (AttributeNamedItem "Name")
    (fun dev_elem prod_elem -> 
      let dev_file = File.ReadAllText(dev_customizations + dev_elem.SelectSingleNode("XamlFileName").InnerText)
      let prod_file = File.ReadAllText(prod_customizations + prod_elem.SelectSingleNode("XamlFileName").InnerText)
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
      )
    (fun id -> 
      add_solution_component proxy sol_id (Guid.Parse id) SolutionComponent.Workflow);
  elim_elem "field security profile" output
    dev_node prod_node 
    "/ImportExportXml/FieldSecurityProfiles/FieldSecurityProfile"
    (Attribute "fieldsecurityprofileid")
    (AttributeNamedItem "name")
    (fun dev_elem prod_elem -> true)
    (fun id -> 
      add_solution_component proxy sol_id (Guid.Parse id) SolutionComponent.FieldSecurityProfile);
  elim_elem "entity relationships" output
    dev_node prod_node 
    "/ImportExportXml/EntityRelationships/EntityRelationship"
    (Attribute "Name")
    (AttributeNamedItem "Name")
    (fun dev_elem prod_elem -> true)
    (fun id -> 
      let req = RetrieveRelationshipRequest ()
      req.Name <- id;
      let resp = proxy.Execute(req) :?> RetrieveRelationshipResponse
      add_solution_component proxy sol_id resp.RelationshipMetadata.MetadataId.Value SolutionComponent.EntityRelationship);
  elim_elem "option set" output
    dev_node prod_node 
    "/ImportExportXml/optionsets/optionset"
    (Attribute "Name")
    (AttributeNamedItem "localizedName")
    (fun dev_elem prod_elem -> true)
    (fun id -> 
      let req = RetrieveOptionSetRequest ()
      req.Name <- id;
      let resp = proxy.Execute(req) :?> RetrieveOptionSetResponse
      add_solution_component proxy sol_id resp.OptionSetMetadata.MetadataId.Value SolutionComponent.OptionSet);
  elim_elem "dashboard" output
    dev_node prod_node 
    "/ImportExportXml/Dashboards/Dashboard"
    (Node "FormId")
    LocalizedNameDescription
    (fun dev_elem prod_elem -> true)
    (fun id -> 
      add_solution_component proxy sol_id (Guid.Parse id) SolutionComponent.Dashboard);
  elim_elem "web resource" output
    dev_node prod_node 
    "/ImportExportXml/WebResources/WebResource"
    (Node "WebResourceId")
    InnerTextDisplayName
    (fun dev_elem prod_elem -> 
      let dev_file = File.ReadAllBytes(dev_customizations + dev_elem.SelectSingleNode("FileName").InnerText)
      let prod_file = File.ReadAllBytes(prod_customizations + prod_elem.SelectSingleNode("FileName").InnerText)
      dev_file = prod_file)
    (fun id -> 
      add_solution_component proxy sol_id (Guid.Parse id) SolutionComponent.WebResource);
  add_all "plugin assembly" output
    dev_node
    "/ImportExportXml/SolutionPluginAssemblies/PluginAssembly"
    (fun elem -> elem.Attributes.GetNamedItem("PluginAssemblyId").Value)
    (fun elem -> elem.Attributes.GetNamedItem("FullName").Value)
    (fun dev_elem -> true)
    (fun id -> 
      add_solution_component proxy sol_id (Guid.Parse id) SolutionComponent.PluginAssembly);
  add_all "plugin step" output
    dev_node
    "/ImportExportXml/SdkMessageProcessingSteps/SdkMessageProcessingStep"
    (fun elem -> elem.Attributes.GetNamedItem("SdkMessageProcessingStepId").Value)
    (fun elem -> elem.Attributes.GetNamedItem("Name").Value)
    (fun dev_elem -> true)
    (fun id -> 
      add_solution_component proxy sol_id (Guid.Parse id) SolutionComponent.PluginStep);
  // (Reports)
  // (regular Sitemap)
  elim_elem "app site map" output
    dev_node prod_node 
    "/ImportExportXml/AppModuleSiteMaps/AppModuleSiteMap"
    (Node "SiteMapUniqueName")
    LocalizedNameDescription
    (fun dev_elem prod_elem -> true)
    (fun id -> 
      add_solution_component proxy sol_id (fetch_sitemap_id proxy id) SolutionComponent.SiteMap);
  elim_elem "app" output
    dev_node prod_node 
    "/ImportExportXml/AppModules/AppModule"
    (Node "UniqueName")
    LocalizedNameDescription
    (fun dev_elem prod_elem -> true)
    (fun id -> 
      add_solution_component proxy sol_id (fetch_app_module_id proxy id) SolutionComponent.AppModule);
    
let to_string (doc : XmlDocument) =
  let ws = new XmlWriterSettings()
  ws.OmitXmlDeclaration <- true;
  let sw = new StringWriter ()
  let writer = XmlWriter.Create(sw, ws)
  doc.Save(writer);
  sw.ToString()

let diff (proxy: IOrganizationService) sol_id (dev_customizations: string) (prod_customizations: string) output =
  log.Verbose "Parsing DEV customizations";
  let dev_doc = XmlDocument ()
  dev_doc.Load (dev_customizations + "/customizations.xml");
  log.Verbose "Parsing PROD customizations";
  let prod_doc = XmlDocument ()
  prod_doc.Load (prod_customizations + "/customizations.xml");
  log.Verbose "Calculating diff";
  elim proxy sol_id dev_customizations prod_customizations dev_doc prod_doc output |> ignore;
  // log.Verbose "Saving";
  // File.WriteAllText (__SOURCE_DIRECTORY__ + @"\diff.xml", to_string dev_doc);

let export file_location complete_solution_name temporary_solution_name (dev:DG.Daxif.Environment) (prod:DG.Daxif.Environment) = 
  log.Info "Starting diff export"
  Directory.CreateDirectory(file_location) |> ignore;
  // Export [complete solution] from DEV and PROD
  let ((dev_proxy, dev_sol), (prod_proxy, prod_sol)) = 
    [| dev; prod |]
    |> Array.Parallel.map (fun env -> 
      log.Verbose "Connecting to %s" env.name;
      let proxy = env.connect().GetProxy()
      Directory.CreateDirectory(file_location + "/" + env.name) |> ignore;
      log.Verbose "Exporting solution '%s' from %s" complete_solution_name env.name;
      let sol = download_with_retry env (file_location + "/" + env.name + "/") complete_solution_name 15 1
      unzip sol;
      (proxy, sol))
    |> function [| dev_sol; prod_sol |] -> (dev_sol, prod_sol) | _ -> failwith "Impossible"
  // Get publisher from [complete solution]
  let complete_sol = fetch_solution dev_proxy complete_solution_name
  // Create new [partial solution] on DEV
  let id = create_solution dev_proxy temporary_solution_name complete_sol.Attributes.["publisherid"]
  try
    // Diff the two exported solutions, add to [partial solution]
    diff dev_proxy temporary_solution_name dev_sol prod_sol Diff;
    // Export [partial solution] from DEV
    log.Verbose "Exporting solution '%s' from %s" temporary_solution_name dev.name;
    let temp_sol = download dev (file_location + "/") temporary_solution_name 15
    // Delete [partial solution] on DEV
    log.Verbose "Deleting solution '%s'" temporary_solution_name;
    dev_proxy.Delete("solution", id);
    log.Info "Done exporting diff solution"
    temporary_solution_name
  with e -> 
    // Delete [partial solution] on DEV
    log.Verbose "Deleting solution '%s'" temporary_solution_name;
    dev_proxy.Delete("solution", id);
    failwith e.Message; 
      
let private add_component_to_solution (proxy: IOrganizationService) solution types workflows (solution_component: Entity) =
  let type_ = (solution_component.Attributes.["componenttype"] :?> OptionSetValue).Value
  let typeString = assoc_right_opt type_ types
  let id = solution_component.Attributes.["objectid"] :?> Guid
  match typeString with
  // Remark, what does fake workflow mean?
    | Some "Workflow" when not (List.contains id workflows) -> log.Verbose " Skipping 'fake' workflow"
    | _ ->
      match typeString with
        | None -> log.Verbose "Adding thing (%i) to solution" type_
        | Some s -> log.Verbose "Adding %s to solution" s
      let req = AddSolutionComponentRequest ()
      req.ComponentType <- type_;
      req.ComponentId <- id;
      req.SolutionUniqueName <- solution;
      proxy.Execute(req) |> ignore
      
let fetch_solution_components proxy (solutionid: Guid) =
  let query = QueryExpression("solutioncomponent")
  query.ColumnSet <- ColumnSet("componenttype", "objectid");
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionid.ToString("B"));
  CrmDataHelper.retrieveMultiple proxy query

let get_workflows proxy (solutionid: Guid) =
  let le = LinkEntity()
  le.JoinOperator <- JoinOperator.Inner;
  le.LinkFromAttributeName <- @"workflowid";
  le.LinkFromEntityName <- @"workflow";
  le.LinkToAttributeName <- @"objectid";
  le.LinkToEntityName <- @"solutioncomponent";
  le.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solutionid);
  let q = QueryExpression("workflow")
  q.LinkEntities.Add(le);
  q.Criteria <- FilterExpression ();
  q.Criteria.AddCondition("type", ConditionOperator.Equal, 1);
  CrmDataHelper.retrieveMultiple proxy q
  |> Seq.toList
  |> List.map (fun e -> e.Id)
      
let private transfer_solution_components (proxy: IOrganizationService) sourceid target =
  log.Verbose "Transfering solution components to '%s'" target;
  let components = fetch_solution_components proxy sourceid
  let types = get_component_types proxy
  let workflows = get_workflows proxy sourceid
  components
  |> Seq.iter (add_component_to_solution proxy target types workflows)

let publish (proxy: IOrganizationService) = 
  log.Info "Publishing changes"
  CrmDataHelper.publishAll proxy

let fetchImportStatusOnce proxy (id: Guid) =
  let query = QueryExpression("importjob")
  query.NoLock <- true;
  query.ColumnSet <- ColumnSet("progress", "completedon", "data");
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("importjobid", ConditionOperator.Equal, id);
  CrmDataHelper.retrieveMultiple proxy query
  |> Seq.tryHead
  |> Option.map (fun a -> 
    a.Attributes.Contains("completedon"), 
    a.Attributes.["progress"] :?> double, 
    a.Attributes.["data"] :?> string)

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
        select_nodes doc "//result[@result='failure']"
        |> Seq.iter (fun a -> log.Verbose "%s" (a.Attributes.GetNamedItem "errortext").Value)
      failwithf "An error occured in import.";
    | Some (true, _, _) -> 
      log.Info "Import succesful.";
    | Some (false, progress, _) -> 
      log.Verbose "Importing: %.1f%%" progress;
      fetchImportStatus proxy id asyncid

let import solution_zip_path complete_solution_name temporary_solution_name (env:DG.Daxif.Environment) = 
  log.Verbose "Connecting to environment %s" env.name;
  let proxy = env.connect().GetProxy()
  // TODO: Remove all Daxif plugin steps
  // Import [partial solution] to TARGET
  let fileBytes = File.ReadAllBytes(solution_zip_path + "/" + temporary_solution_name + ".zip")
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
  let temp = solution_zip_path + "/" + temporary_solution_name
  unzip temp;
  log.Verbose "Parsing TEMP customizations";
  let xml = XmlDocument ()
  xml.Load (temp + "/customizations.xml");
  log.Verbose "Setting workflow states";
  let workflows = select_nodes xml "/ImportExportXml/Workflows/Workflow"
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
    proxy.Execute(req) |> ignore;
    )
  // Run through solution components of [partial solution], add to [complete solution] on TARGET
  let sol = fetch_solution proxy temporary_solution_name

  publish proxy;
  stopWatch.Stop()
  log.Info "Downtime: %.1f minutes" stopWatch.Elapsed.TotalMinutes;
  transfer_solution_components proxy sol.Id complete_solution_name
  // Delete [partial solution] on TARGET
  log.Verbose "Deleting solution '%s'" temporary_solution_name;
  proxy.Delete("solution", sol.Id);
    