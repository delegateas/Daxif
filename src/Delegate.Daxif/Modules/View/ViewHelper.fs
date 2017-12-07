module internal DG.Daxif.Modules.View.ViewHelper

open System
open Microsoft.Xrm.Sdk
open DG.Daxif.Common
open Microsoft.Xrm.Sdk.Query
open System.Xml.Linq
open AllowedConditions
open TypeDeclarations
open Microsoft.Xrm.Sdk.Client

let getOrdering = 
  function 
  | OrderType.Ascending -> false
  | OrderType.Descending -> true
  | _ -> failwith "Unknown ordering"

let getNewXml p (view: View) layout fetch =
  let newLayout = XmlManipulator.setColumns view.columns layout
  let newFetch =
    XmlManipulator.setAttr view.attributes fetch
    |> XmlManipulator.setOrder view.order
    |> XmlManipulator.setFilter p view.filter
    |> XmlManipulator.setLinks p view.links
  newLayout, newFetch
  
let getLayoutAndFetch p guid = 
  let entity = CrmData.CRUD.retrieve p ViewLogicalName guid
  let attr = entity.Attributes
  XDocument.Parse(string attr.["layoutxml"]), XDocument.Parse(string attr.["fetchxml"])
  
let removeByKey key list = 
  List.foldBack (fun (k, v) list -> 
    if k = key then list
    else (k, v) :: list) list []
  
let updateByKey key newValue list = 
  List.foldBack (fun (k, v) list -> 
    if k = key then (k, newValue) :: list
    else (k, v) :: list) list []

let hasSameNameAndTo entityName attributeName = 
  function
  | Link.Entity entity ->
    entity.LinkToEntityName = entityName && entity.LinkToAttributeName = attributeName
  | Link.Xml xml ->
    xml.Attribute(XmlManipulator.xn "name").Value = entityName && 
    xml.Attribute(XmlManipulator.xn "to").Value = attributeName

let getAliasFromRel refedEnt refedAttr refingEnt refingAttr =
  refedEnt + refedAttr + refingEnt + refingAttr

let getLinkFromRel (EntityRelationship.Rel(refedEnt, refedAttr, refingEnt, refingAttr)) 
  (columns : IEntityAttribute list) =
  let link = LinkEntity(refingEnt, refedEnt, refingAttr, refedAttr, JoinOperator.LeftOuter)
  link.EntityAlias <- getAliasFromRel refedEnt refedAttr refingEnt refingAttr
  let columns' = columns |> List.map (fun c -> c.Name)
  link.Columns.AddColumns(List.toArray columns')
  link

let rec insertList value index list =
  match index, list with
  | 0, [] -> [value]
  | 0, xs -> value::xs
  | i, x::xs -> x::insertList value (i - 1) xs
  | i, [] -> failwith "index out of range"

let updateView (p: OrganizationServiceProxy) (view: View) =
  let entity = CrmData.CRUD.retrieve p ViewLogicalName view.id
  let attr = entity.Attributes
  let newLayout, newFetch = 
    getNewXml p view (XDocument.Parse(string attr.["layoutxml"])) 
      (XDocument.Parse(string attr.["fetchxml"]))
  let toUpdate = new Entity(entity.LogicalName)
  toUpdate.Id <- entity.Id
  toUpdate.Attributes.Add("layoutxml", string newLayout)
  toUpdate.Attributes.Add("fetchxml", string newFetch)
  CrmData.CRUD.update p toUpdate |> ignore

let updateViewAndPublish proxyGen (view: View) =
  use p = proxyGen()
  updateView p view  
  CrmDataHelper.publishAll p

let updateViewList proxyGen (views: View list) = 
  use p = proxyGen()
  List.iter (updateView p) views
  CrmDataHelper.publishAll p

let parse proxyGen guid =
  use p = proxyGen()

  let (layout, fetch) = getLayoutAndFetch p guid
  { id = guid
    attributes = XmlManipulator.getAttr fetch
    columns = XmlManipulator.getColumns layout
    order = XmlManipulator.getOrder fetch
    filter = XmlManipulator.getFilter fetch
    links = XmlManipulator.getLinks fetch }

let addColumn (column: IEntityAttribute) width index (view: View) = 
  if Set.contains column.Name view.attributes then 
    failwith ("Already have a column with logicalname '" + column.Name + "', choose another or remove the existing one")
  if index > view.columns.Length then failwith ("index '" + string index + "' was out of bounds for columns of length '" + string view.columns.Length + "'")
  { view with attributes = Set.add column.Name view.attributes
              columns =  insertList (column.Name, width) index view.columns }

let addColumnFirst (column: IEntityAttribute) width (view: View) =
  addColumn column width 0 view

let addColumnLast (column: IEntityAttribute) width (view: View) =
  addColumn column width (view.columns.Length ) view
  
let removeColumn (column: IEntityAttribute) (view: View) =
  { view with attributes = Set.remove column.Name view.attributes
              columns = removeByKey column.Name view.columns
              order = removeByKey column.Name view.order }
  
let addOrdering (column: IEntityAttribute) (ordering: OrderType) (view: View) = 
  if view.order.Length = 2 then failwith "Already have 2 sortings, remove an existing one to add another"
  else { view with order = (column.Name, getOrdering ordering) :: view.order }
  
let removeOrdering (column: IEntityAttribute) (view: View) = 
  { view with order = removeByKey column.Name view.order }
  
let changeWidth (column: IEntityAttribute) width (view: View) = 
  if Set.contains column.Name view.attributes |> not then failwith ("Width can't be changed, since no column with logicalname '" + column.Name + "' exists")
  else { view with columns = updateByKey column.Name width view.columns }
  
let setFilter (filter: FilterExpression) (view: View) = { view with filter = Filter.Expr filter }
  
let andFilters (filter: FilterExpression) (view: View) = 
  let wrap = FilterExpression()
  wrap.FilterOperator <- LogicalOperator.And
  { view with filter = Filter.Nested(wrap, [Filter.Expr filter; view.filter]) }
  
let orFilters (filter: FilterExpression) (view: View) = 
  let wrap = FilterExpression()
  wrap.FilterOperator <- LogicalOperator.Or
  { view with filter = Filter.Nested(wrap, [Filter.Expr filter; view.filter]) }

let removeFilter (view: View) = 
  {view with filter = Filter.Empty}

let addLink (rel: EntityRelationship) (columns: IEntityAttribute list) (columnWidths: int list) 
  (indexes: int list) (view: View) =
  let link = getLinkFromRel rel columns
  let alias = link.EntityAlias
  if Map.containsKey alias view.links 
  then failwith ("A link with alias '" + alias + "' already exists. Check if this attribute is already referenced")
  if Map.exists (fun _ (l: Link) -> 
    hasSameNameAndTo link.LinkToEntityName link.LinkFromAttributeName l) view.links
    then failwith ("There already exists a link entity with the name '" + 
                  link.LinkToEntityName + 
                  "' which points to '" + 
                  link.LinkToAttributeName + "'")
  if link.Columns.Columns.Count <> columnWidths.Length then failwith "Not the same amount of columns and columnwidths"
  if link.Columns.Columns.Count <> indexes.Length then failwith "Not the same amount of columns and indexes"
  {view with 
    links = Map.add alias (Link.Entity link) view.links
    columns = List.zip columns columnWidths
              |> List.map (fun (column : IEntityAttribute, width) -> 
                let name = column.Name
                (link.EntityAlias + "." + name, width))
              |> List.fold2 (fun columns index column -> insertList column index columns) view.columns indexes
              }

let addLinkFirst (rel: EntityRelationship) (columns: IEntityAttribute list) (columnWidths: int list) 
  (view : View) =
  addLink rel columns columnWidths [0..(columnWidths.Length - 1)] view

let addLinkLast (rel: EntityRelationship) (columns: IEntityAttribute list) (columnWidths: int list) 
  (view : View) =
  addLink rel columns columnWidths [view.columns.Length..(view.columns.Length + columnWidths.Length - 1)] view

let removeLink (EntityRelationship.Rel(refedEnt, refedAttr, refingEnt, refingAttr)) (view: View) =
  let alias = getAliasFromRel refedEnt refedAttr refingEnt refingAttr
  {view with 
    links = Map.remove alias view.links
    columns = List.filter (fun (name,_) ->
      (name.Split([|'.'|]).[0] <> alias)) view.columns}

let changeId guid (view: View) = {view with id = guid}    

let initFilter operator = FilterExpression(operator)

let addCondition0 (attributeEntity: EntityAttribute<_,'b>) (operator: 'b)
  (filter: FilterExpression) =
  filter.AddCondition(attributeEntity.Name, parseCondition operator)
  filter

let addCondition1 (attributeEntity: EntityAttribute<'a,'b>) (operator: 'b)
    (arg: 'a) (filter: FilterExpression) =
  let operator = parseCondition operator 
  match (box arg) with
  | :? Enum -> 
    filter.AddCondition(attributeEntity.Name, operator, (box arg) :?> int)
  | _ -> 
    filter.AddCondition(attributeEntity.Name, operator, arg)
  filter

let addCondition2 (attributeEntity: EntityAttribute<'a,'b>) (operator: 'b)
    (arg1: 'a) (arg2: 'a) (filter: FilterExpression) =
  let operator = parseCondition operator 
  match (box arg1) with
  | :? Enum ->
    filter.AddCondition(attributeEntity.Name, 
      operator, (box (arg1: 'a)) :?> int, (box (arg2: 'a)) :?> int)
  | _ ->
    filter.AddCondition(attributeEntity.Name, operator, arg1, arg2)
  filter

let addConditionMany (attributeEntity: EntityAttribute<'a,'b>) (operator: 'b)
    (arg: 'a list) (filter: FilterExpression) =
  let operator = parseCondition operator 
  match (box arg.Head) with
  | :? Enum-> 
    filter.AddCondition(attributeEntity.Name, 
      operator, List.map (fun arg -> (box arg) :?> int) arg)
  | _ -> 
    filter.AddCondition(attributeEntity.Name, operator, arg)
  filter

let addFilter (toAdd: FilterExpression) (filter: FilterExpression) = 
  filter.AddFilter(toAdd)
  filter
