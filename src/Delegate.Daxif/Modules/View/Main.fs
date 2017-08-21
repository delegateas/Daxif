module DG.Daxif.Modules.View.Main

open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open TypeDeclarations
open DG.Daxif

let generateFiles org ap usr (pwd: string) domain daxifRoot entities solutions log =
  let log' = ConsoleLogger log
  let pwd' = String.replicate pwd.Length "*"
  log'.WriteLine(LogLevel.Info, daxifVersion)
  log'.WriteLine(LogLevel.Info, @"Generate view extender files:")
  log'.WriteLine(LogLevel.Verbose, @"Organization: " + org.ToString())
  logAuthentication ap usr pwd' domain log'
  let ac = CrmAuth.getCredentials ap usr pwd domain
  let dir = daxifRoot ++ generationFolder
  log'.WriteLine(LogLevel.Verbose, @"Generating to folder: " + dir)
  ensureDirectory (dir)
  Generator.generateFiles org ac daxifRoot entities solutions log'
  log'.WriteLine
    (LogLevel.Info, @"The ViewExtender files were generated successfully")

let updateView org ap usr (pwd: string) domain view =
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