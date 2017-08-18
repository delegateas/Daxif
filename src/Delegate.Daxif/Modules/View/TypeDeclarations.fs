module DG.Daxif.Modules.View.TypeDeclarations

open Microsoft.Xrm.Sdk.Query
open System.Xml.Linq
open System

[<Literal>]
let ViewLogicalName = "savedquery"

[<Literal>]
let generationFolder = "viewExtenderData"

type Filter = Empty | Expr of FilterExpression | Xml of XElement | Nested of FilterExpression * Filter list
type Link = Entity of LinkEntity | Xml of XElement

type View = 
  { id : Guid
    attributes : Set<string>
    columns : (string * int) list
    order : (string * bool) list
    filter : Filter
    links : Map<string, Link> }

type IEntityAttribute = interface end
type EntityAttribute<'a, 'b>(name : string) =
  interface IEntityAttribute
  member this.Name = name

type EntityRelationship = Rel of string * string * string * string

