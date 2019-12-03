module DG.Daxif.Modules.Solution.DiffAdder

open DG.Daxif.Common
open InternalUtility
open System.IO
open System.IO.Compression
open System
open Microsoft.Xrm.Sdk.Query
open System.Threading
open System.Xml
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open DiffFetcher
open Microsoft.Crm.Sdk.Messages
open Domain


let createSolution (proxy: IOrganizationService) temporary_solution_name publisher = 
  log.Verbose "Creating solution '%s'" temporary_solution_name;
  let upd = Entity ("solution")
  upd.Attributes.["uniquename"] <- temporary_solution_name;
  upd.Attributes.["friendlyname"] <- "Temporary. For deploy";
  upd.Attributes.["publisherid"] <- publisher;
  proxy.Create upd

let createEntityComponent (proxy: IOrganizationService) sol_id comp_id (comp_type: EntityComponent) =
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


let createSolutionComponent (proxy: IOrganizationService) sol_id comp_id (comp_type: SolutionComponent) =
  let compTypeId = LanguagePrimitives.EnumToValue comp_type
  let req = 
    AddSolutionComponentRequest (
      AddRequiredComponents = false,
      ComponentId = comp_id,
      ComponentType = compTypeId,
      SolutionUniqueName = sol_id
    )
  proxy.Execute(req) |> ignore

let selectNodes (node: XmlNode) (xpath: string) =
  node.SelectNodes(xpath)
  |> Seq.cast<XmlNode>

let addAll type_ output (dev_node: XmlNode) dev_path dev_id dev_readable extra_check callback =
  let dev_elems = selectNodes dev_node dev_path
  dev_elems
  |> Seq.iter (fun dev_elem ->
    let id = dev_id dev_elem
    let name = dev_readable dev_elem
    log.Verbose "Adding %s: %s" type_ name;
    callback id;
    )

let addComponentToSolution (proxy: IOrganizationService) solution types workflows (solution_component: Entity) =
  let type_ = (solution_component.Attributes.["componenttype"] :?> OptionSetValue).Value
  let typeString = assocRightOption type_ types
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

let transferSolutionComponents (proxy: IOrganizationService) sourceid target =
  log.Verbose "Transfering solution components to '%s'" target;
  let components = fetchSolutionComponents proxy sourceid
  let types = fetchComponentType proxy
  let workflows = fetchWorkflowIds proxy sourceid
  components
  |> Seq.iter (addComponentToSolution proxy target types workflows)