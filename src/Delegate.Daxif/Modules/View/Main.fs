module DG.Daxif.Modules.View.Main

open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open TypeDeclarations
open DG.Daxif

let generateFiles proxyGen daxifRoot entities solutions =
  let dir = daxifRoot ++ generationFolder
  log.Verbose "Generating to folder: %s" dir
  ensureDirectory dir
  Generator.generateFiles proxyGen daxifRoot entities solutions
  log.Info "The ViewExtender files were generated successfully"

let updateView proxyGen view = ViewHelper.updateView proxyGen view
let updateViewList proxyGen views = ViewHelper.updateViewList proxyGen views
let parse proxyGen guid = ViewHelper.parse proxyGen guid

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
let addRelatedColumn = ViewHelper.addLink
let addRelatedColumnFirst = ViewHelper.addLinkFirst
let addRelatedColumnLast = ViewHelper.addLinkLast
let removeLink = ViewHelper.removeLink
let changeId = ViewHelper.changeId
let initFilter = ViewHelper.initFilter
let addCondition = ViewHelper.addCondition
let addCondition2 = ViewHelper.addCondition2
let addConditionMany = ViewHelper.addConditionMany
let addFilter = ViewHelper.addFilter