module DG.Daxif.Modules.Solution.DiffAdder

open DG.Daxif.Common
open InternalUtility
open System.IO
open System
open System.Xml
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open DiffFetcher
open Microsoft.Crm.Sdk.Messages
open Domain
open System.Text.RegularExpressions

let createSolution (proxy: IOrganizationService) temporary_solution_name publisher = 
  log.Verbose "Creating solution '%s'" temporary_solution_name;
  let upd = Entity ("solution")
  upd.Attributes.["uniquename"] <- temporary_solution_name;
  upd.Attributes.["friendlyname"] <- "Temporary. For deploy";
  upd.Attributes.["publisherid"] <- publisher;
  proxy.Create upd

let createEntityComponentRequest (*(proxy: IOrganizationService)*) sol_id comp_id (comp_type: EntityComponent) =
  let compTypeId = LanguagePrimitives.EnumToValue comp_type
  AddSolutionComponentRequest (
    AddRequiredComponents = false,
    ComponentId = comp_id,
    ComponentType = compTypeId,
    DoNotIncludeSubcomponents = true,
    SolutionUniqueName = sol_id
  )

let createSolutionComponentRequest (*(proxy: IOrganizationService)*) sol_id comp_id (comp_type: SolutionComponent) =
  let compTypeId = LanguagePrimitives.EnumToValue comp_type
  AddSolutionComponentRequest (
    AddRequiredComponents = false,
    ComponentId = comp_id,
    ComponentType = compTypeId,
    SolutionUniqueName = sol_id
  )



let addComponentToSolution (proxy: IOrganizationService) solution types workflows (solution_component: Entity) =
  let typeId = (solution_component.Attributes.["componenttype"] :?> OptionSetValue).Value
  let typeString = Map.tryFind typeId types
  let id = solution_component.Attributes.["objectid"] :?> Guid
  match typeString with
    // Remark, what does fake workflow mean?
    | Some "Workflow" when not (List.contains id workflows) -> log.Verbose " Skipping 'fake' workflow"
    | _ ->
      match typeString with
        | None -> log.Verbose "Adding thing (%i) to solution" typeId
        | Some s -> log.Verbose "Adding %s to solution" s
      let req = AddSolutionComponentRequest ()
      req.ComponentType <- typeId;
      req.ComponentId <- id;
      req.SolutionUniqueName <- solution;
      proxy.Execute(req) |> ignore

let transferSolutionComponents (proxy: IOrganizationService) sourceid target =
  log.Verbose "Transfering solution components to '%s'" target;
  let components = fetchSolutionComponents proxy sourceid
  let types = fetchComponentTypes proxy
  let workflows = fetchWorkflowIds proxy sourceid
  components
  |> Seq.iter (addComponentToSolution proxy target types workflows)

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
  
let workflowGuidReplace ((dev_path, prod_path): (string*string)) (dev_elem: XmlNode) (prod_elem: XmlNode) =
  try
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
  with e ->
    // If we end up here, either dev file or prod file does not exist, and there is a difference between the two
    false

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

let callbackFun diffSolutionUniqueName (resp: RetrieveEntityResponse option) id = function 
  | EntityMetadataCallback -> 
      createEntityComponentRequest diffSolutionUniqueName resp.Value.EntityMetadata.MetadataId.Value EntityComponent.EntityMetaData
  | RibbonCallback ->
      createSolutionComponentRequest diffSolutionUniqueName resp.Value.EntityMetadata.MetadataId.Value SolutionComponent.Ribbon
  | CustomCallback callback -> 
      callback id
  | EntityComponentCallback comp -> 
      createEntityComponentRequest diffSolutionUniqueName (Guid.Parse id) comp
  | SolutionComponentCallback comp-> 
      createSolutionComponentRequest diffSolutionUniqueName (Guid.Parse id) comp

let addAll type_ (dev_node: XmlNode) dev_path (devId: ReadableName) (devReadable: ReadableName) (addType: SolutionComponent) diffSolutionUniqueName =
  let dev_elems = selectNodes dev_node dev_path
  dev_elems
  |> Seq.map (fun devElem ->
    let id = GetReadableName devElem devId
    let name = GetReadableName devElem devReadable
    log.Verbose "Adding %s: %s" type_ name;
    Some (createSolutionComponentRequest diffSolutionUniqueName (Guid.Parse id) addType);
  )

let executeImportRequestWithProgress (proxy: IOrganizationService) (fileBytes: byte []) = 
  log.Info "Importing solution";
  let importid = Guid.NewGuid()
  let req = ImportSolutionRequest ()
  req.CustomizationFile <- fileBytes;
  req.ImportJobId <- importid;
  let async_req = ExecuteAsyncRequest ()
  async_req.Request <- req;
  let resp = proxy.Execute(async_req) :?> ExecuteAsyncResponse
  fetchImportStatus proxy importid resp.AsyncJobId;

let setWorkflowStates (proxy: IOrganizationService) temp = 
  let xml = XmlDocument ()
  xml.Load (temp + "/customizations.xml")

  log.Verbose "Setting workflow states"

  selectNodes xml "/ImportExportXml/Workflows/Workflow"
  |> Seq.iter (fun e ->
    let id = e.Attributes.GetNamedItem("WorkflowId").Value
    let state = int (e.SelectSingleNode("StateCode").InnerText)
    let status = int (e.SelectSingleNode("StatusCode").InnerText)
    let req = 
      SetStateRequest(
        State = new OptionSetValue(state), 
        Status = new OptionSetValue(status), 
        EntityMoniker = new EntityReference("workflow", Guid.Parse(id))
      )
    proxy.Execute(req) |> ignore
  )

// Specific functions for adding different components

type DiffFunction<'a> = string -> string -> id_node -> ReadableName -> ExtraChecks -> CallbackKind -> seq<'a>

(* entity components *)
let entityMetadataHandler (diff: DiffFunction<'a>) = 
  diff "entity info" "EntityInfo/entity/*[not(self::attributes)]" 
    (Custom (fun id -> "EntityInfo/entity/"+id))
    ElementName
    NoExtra
    EntityMetadataCallback

let entityRibbonHandler (diff: DiffFunction<'a>) = 
  diff "ribbon" "RibbonDiffXml"
    (Custom (fun id -> "RibbonDiffXml"))
    (Static "Ribbon")
    NoExtra
    RibbonCallback

let entityAttributeHandler (resp: RetrieveEntityResponse) diffSolutionUniqueName (diff: DiffFunction<'a>) = 
  diff "attribute" "EntityInfo/entity/attributes/attribute"
    (Attribute "PhysicalName")
    (AttributeNamedItem "PhysicalName")
    NoExtra
    (CustomCallback (fun id -> 
        resp.EntityMetadata.Attributes
        |> Array.find (fun a -> a.SchemaName = id)
        |> fun a -> createEntityComponentRequest diffSolutionUniqueName a.MetadataId.Value EntityComponent.Attribute)
    )

let entityFormHandler (diff: DiffFunction<'a>) = 
  diff "form" "FormXml/forms/systemform"
    (Node "formid")
    LocalizedNameDescription
    NoExtra
    (EntityComponentCallback EntityComponent.Form)
  
let entityViewHandler (diff: DiffFunction<'a>) = 
  diff "view" "SavedQueries/savedqueries/savedquery"
    (Node "savedqueryid")
    LocalizedNameDescription
    NoExtra
    (EntityComponentCallback EntityComponent.View)
  
let entityChartHandler (diff: DiffFunction<'a>) = 
  diff "chart" "Visualizations/visualization"
    (Node "savedqueryvisualizationid")
    LocalizedNameDescription
    NoExtra
    (EntityComponentCallback EntityComponent.Chart)

(* solution components *)
let solutionRoleHandler (diff: DiffFunction<'a>) = 
  diff "role" "/ImportExportXml/Roles/Role"
    (Attribute "id")
    (AttributeNamedItem "name")
    NoExtra
    (SolutionComponentCallback SolutionComponent.Role)

let solutionWorkflowHandler (diff: DiffFunction<'a>) = 
  diff "workflow" "/ImportExportXml/Workflows/Workflow"
    (Attribute "WorkflowId")
    (AttributeNamedItem "Name")
    WorkflowGuidReplace
    (SolutionComponentCallback SolutionComponent.Workflow)

let solutionFieldSecurityProfileHandler (diff: DiffFunction<'a>) = 
  diff "field security profile" "/ImportExportXml/FieldSecurityProfiles/FieldSecurityProfile"
    (Attribute "fieldsecurityprofileid")
    (AttributeNamedItem "name")
    NoExtra
    (SolutionComponentCallback SolutionComponent.FieldSecurityProfile)
    
let solutionEntityRelationshipHandler (proxy: IOrganizationService) diffSolutionUniqueName (diff: DiffFunction<'a>) = 
  diff "entity relationships" "/ImportExportXml/EntityRelationships/EntityRelationship"
    (Attribute "Name")
    (AttributeNamedItem "Name")
    NoExtra
    (CustomCallback (fun id -> 
      let req = RetrieveRelationshipRequest ()
      req.Name <- id;
      let resp = proxy.Execute(req) :?> RetrieveRelationshipResponse
      createSolutionComponentRequest diffSolutionUniqueName resp.RelationshipMetadata.MetadataId.Value SolutionComponent.EntityRelationship);
    )

let solutionOptionSetHandler (proxy: IOrganizationService) diffSolutionUniqueName (diff: DiffFunction<'a>) = 
  diff "option set" "/ImportExportXml/optionsets/optionset"
    (Attribute "Name")
    (AttributeNamedItem "localizedName")
    NoExtra
    (CustomCallback (fun id -> 
        let req = RetrieveOptionSetRequest ()
        req.Name <- id;
        let resp = proxy.Execute(req) :?> RetrieveOptionSetResponse
        createSolutionComponentRequest diffSolutionUniqueName resp.OptionSetMetadata.MetadataId.Value SolutionComponent.OptionSet);
     )

let solutionDashboardHandler (diff: DiffFunction<'a>) = 
  diff "dashboard" "/ImportExportXml/Dashboards/Dashboard"
    (Node "FormId")
    LocalizedNameDescription
    NoExtra
    (SolutionComponentCallback SolutionComponent.Dashboard)

let solutionWebResourceHandler (diff: DiffFunction<'a>) = 
  diff "web resource" "/ImportExportXml/WebResources/WebResource"
    (Node "WebResourceId")
    InnerTextDisplayName
    WebResourceByteDiff
    (SolutionComponentCallback SolutionComponent.WebResource)
    
let solutionPluginAssemblyHandler devNode diffSolutionUniqueName (diff: DiffFunction<'a>) = 
  addAll "plugin assembly" devNode
    "/ImportExportXml/SolutionPluginAssemblies/PluginAssembly"
    (AttributeNamedItem "PluginAssemblyId")
    (AttributeNamedItem "FullName")
    SolutionComponent.PluginAssembly
    diffSolutionUniqueName

let solutionPluginStepHandler devNode diffSolutionUniqueName (diff: DiffFunction<'a>) = 
  addAll "plugin step" devNode
    "/ImportExportXml/SdkMessageProcessingSteps/SdkMessageProcessingStep"
    (AttributeNamedItem "SdkMessageProcessingStepId")
    (AttributeNamedItem "Name")
    SolutionComponent.PluginStep
    diffSolutionUniqueName

let solutionAppSiteMapHandler proxy diffSolutionUniqueName (diff: DiffFunction<'a>) = 
  diff "app site map" "/ImportExportXml/AppModuleSiteMaps/AppModuleSiteMap"
    (Node "SiteMapUniqueName")
    LocalizedNameDescription
    NoExtra
    (CustomCallback (fun id -> 
      createSolutionComponentRequest diffSolutionUniqueName (fetchSitemapId proxy id) SolutionComponent.SiteMap))

let solutionAppHandler proxy diffSolutionUniqueName (diff: DiffFunction<'a>) = 
  diff "app" "/ImportExportXml/AppModules/AppModule"
    (Node "UniqueName")
    LocalizedNameDescription
    NoExtra
    (CustomCallback (fun id -> 
      createSolutionComponentRequest diffSolutionUniqueName (fetchAppModuleId proxy id) SolutionComponent.AppModule))