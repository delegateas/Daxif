module internal DG.Daxif.Modules.View.XmlManipulator

open Microsoft.Crm.Sdk.Messages
open System.Xml.Linq
open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Query
open TypeDeclarations

let xn s = XName.Get(s)
  
let getColumns (xml: XDocument) =
  xml.Element(xn "grid").Element(xn "row").Elements(xn "cell")
  |> Seq.map (fun (e: XElement) -> e.Attribute(xn "name").Value, e.Attribute(xn "width").Value |> int)
  |> List.ofSeq
  
let getAttr (xml: XDocument) =
  xml.Element(xn "fetch").Element(xn "entity").Elements(xn "attribute")
  |> Seq.map (fun (e: XElement) -> e.Attribute(xn "name").Value)
  |> Set.ofSeq
  
let getOrder (xml: XDocument) =
  xml.Element(xn "fetch").Element(xn "entity").Elements(xn "order")
  |> Seq.map (fun (e: XElement) -> 
    e.Attribute(xn "attribute").Value, Boolean.Parse(e.Attribute(xn "descending").Value))
  |> List.ofSeq
  
let getFilter (xml: XDocument) =
  let filter = (xml.Element(xn "fetch").Element(xn "entity").Element(xn "filter"))
  if filter = null || filter.IsEmpty then Filter.Empty else Filter.Xml(filter)

  
let getLinks (xml: XDocument) =
  xml.Element(xn "fetch").Element(xn "entity").Elements(xn "link-entity")
  |> Seq.map (fun e -> e.Attribute(xn "alias").Value, Link.Xml e)
  |> Map.ofSeq
  
let addColumns columns (xml: XDocument) =
  List.fold (fun (doc: XDocument) (name: string, width: int) -> 
    let root = new XElement(xn "cell")
    root.Add(new XAttribute(xn "name", name))
    root.Add(new XAttribute(xn "width", width))
    doc.Element(xn "grid").Element(xn "row").Add(root)
    doc) xml columns
  
let setColumns columns (xml: XDocument) =
  xml.Element(xn "grid").Element(xn "row").Elements(xn "cell").Remove()
  addColumns columns xml

  
let setAttr attr (xml: XDocument) =
  xml.Element(xn "fetch").Element(xn "entity").Elements(xn "attribute").Remove()
  Set.fold (fun (xml: XDocument) name -> 
    let root = new XElement(xn "attribute")
    root.Add(new XAttribute(xn "name", name))
    xml.Element(xn "fetch").Element(xn "entity").Add(root)
    xml) xml attr
  
let setOrder order (xml: XDocument) =
  xml.Element(xn "fetch").Element(xn "entity").Elements(xn "order").Remove()
  List.fold (fun (xml: XDocument) (attribute, descending) -> 
    let root = new XElement(xn "order")
    root.Add(new XAttribute(xn "attribute", attribute))
    root.Add(new XAttribute(xn "descending", descending))
    xml.Element(xn "fetch").Element(xn "entity").Add(root)
    xml) xml order

let getXmlFromFilterExp (proxy: IOrganizationService) logicalname (filter: FilterExpression) = 
  let query = QueryExpression(logicalname)
  query.Criteria <- filter
  let req = QueryExpressionToFetchXmlRequest()
  req.Query <- query
  let resp = proxy.Execute(req) :?> QueryExpressionToFetchXmlResponse
  let doc = XDocument.Parse(resp.FetchXml)
  doc.Element(xn "fetch").Element(xn "entity").Element(xn "filter")

let rec getXmlFromFilterStructure proxy logicalname = function
  | Filter.Empty -> XElement(xn "filter")
  | Filter.Expr fe -> getXmlFromFilterExp proxy logicalname fe
  | Filter.Xml xml -> xml
  | Filter.Nested(wrap, filters) -> 
    let wrapper = new XElement(xn "filter")
    wrapper.Add(new XAttribute(xn "type", wrap.FilterOperator.ToString().ToLower()))
    List.fold (fun (wrapper: XElement) (filter: Filter) -> 
      wrapper.Add(getXmlFromFilterStructure proxy logicalname filter)
      wrapper) wrapper filters
  
let getXmlFromLinkEntityExp (proxy: IOrganizationService) (link: LinkEntity) = 
  let query = QueryExpression(link.LinkFromEntityName)
  query.LinkEntities.Add(link)
  let req = QueryExpressionToFetchXmlRequest()
  req.Query <- query
  let resp = proxy.Execute(req) :?> QueryExpressionToFetchXmlResponse
  let doc = XDocument.Parse(resp.FetchXml)
  doc.Element(xn "fetch").Element(xn "entity").Element(xn "link-entity")
  
let setFilter proxy (filterStruct: Filter) (xml: XDocument) =
  if xml.Element(xn "fetch").Element(xn "entity").Element(xn "filter") <> null then 
    xml.Element(xn "fetch").Element(xn "entity").Element(xn "filter").Remove()
  let primaryEntity = xml.Element(xn "fetch").Element(xn "entity").Attribute(xn "name").Value
  let filter = getXmlFromFilterStructure proxy primaryEntity filterStruct
  if not filter.IsEmpty then
    xml.Element(xn "fetch").Element(xn "entity").Add(filter)
  xml
  
let setLinks proxy links (xml: XDocument)  =
  if xml.Element(xn "fetch").Element(xn "entity").Elements(xn "link-entity") <> null then 
    xml.Element(xn "fetch").Element(xn "entity").Elements(xn "link-entity").Remove()
  Map.fold (fun (xml: XDocument) _ (link: Link) ->
    match link with
    | Link.Xml xml' ->
      xml.Element(xn "fetch").Element(xn "entity").Add(xml')
      xml                
    | Link.Entity entity ->
      let linkXml = getXmlFromLinkEntityExp proxy entity
      xml.Element(xn "fetch").Element(xn "entity").Add(linkXml)
      xml
    ) xml links