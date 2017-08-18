module DG.Daxif.Modules.View.Main

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Modules
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open System
open Microsoft.Xrm.Sdk.Query
open System.Xml.Linq
open TypeDeclarations

let generate org ap usr pwd domain daxifRoot entities solutions log = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  ensureDirectory (daxifRoot ++ generationFolder)
  Generator.generateFiles org ac daxifRoot entities solutions

let updateView org ap usr pwd domain view = 
  let ac = CrmAuth.getCredentials ap usr pwd domain
  ViewHelper.updateView org ac view

let updateViewList org ap usr pwd domain views =
  let ac = CrmAuth.getCredentials ap usr pwd domain
  ViewHelper.updateViewList org ac views

let parse org ap usr pwd domain guid =
  let ac = CrmAuth.getCredentials ap usr pwd domain
  ViewHelper.parse org ac guid

let addColumn = ViewHelper.addColumn
let addColumnFirst = ViewHelper.addColumnFirst
let addColumnLast = ViewHelper.addColumnLast
let removeColumn = ViewHelper.removeColumn
let addOrdering = ViewHelper.addOrdering
let removeOrdering = ViewHelper.removeOrdering
let changeWidth = ViewHelper.changeWidth
let setFilter = ViewHelper.setFilter  
let andFilters = ViewHelper.andFilters 
let orFilters = ViewHelper.orFilters
let removeFilter = ViewHelper.removeFilter
let addLink = ViewHelper.addLink
let addLinkFirst = ViewHelper.addLinkFirst
let addLinkLast = ViewHelper.addLinkLast
let removeLink = ViewHelper.removeLink
let extend = ViewHelper.extend
let initFilter = ViewHelper.initFilter
let addCondition = ViewHelper.addCondition
let addCondition2 = ViewHelper.addCondition2
let addConditionMany = ViewHelper.addConditionMany
let addFilter = ViewHelper.addFilter