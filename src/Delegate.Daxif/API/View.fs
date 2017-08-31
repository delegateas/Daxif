namespace DG.Daxif

open DG.Daxif.Common
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Modules.View
open Utility
open DG.Daxif.Modules.View.TypeDeclarations
open Microsoft.Xrm.Sdk.Query

type View private () =

  /// <summary>Generates the files needed for View Extender</summary>
  /// <param name="env">Environment the action should be performed against.</param>
  static member GenerateFiles(env: Environment, daxifRoot: string, ?entities: string[], ?solutions: string[], ?logLevel: LogLevel) =
    let proxyGen = env.connect(log).GetProxy
    log.setLevelOption logLevel
    Main.generateFiles proxyGen daxifRoot entities solutions
  
  static member UpdateView (env: Environment) (view: TypeDeclarations.View) =
    let proxyGen = env.connect(log).GetProxy
    Main.updateView proxyGen view

  static member UpdateViewList (env: Environment) (views: TypeDeclarations.View list) = 
    let proxyGen = env.connect(log).GetProxy
    Main.updateViewList proxyGen views

  static member Parse (env: Environment) (guid: System.Guid) =
   let proxyGen = env.connect(log).GetProxy
   Main.parse proxyGen guid

  static member AddColumn (column: IEntityAttribute) width index (view: TypeDeclarations.View) = 
    Main.addColumn column width index view
  static member AddColumnFirst (column: IEntityAttribute) width (view: TypeDeclarations.View) =
    Main.addColumnFirst column width view
  static member AddColumnLast (column: IEntityAttribute) width (view: TypeDeclarations.View) =
    Main.addColumnLast column width view
  static member RemoveColumn (column: IEntityAttribute) (view: TypeDeclarations.View) =
    Main.removeColumn column view
  static member AddOrdering (column: IEntityAttribute) (ordering: OrderType) (view: TypeDeclarations.View) = 
    Main.addOrdering column ordering view
  static member RemoveOrdering (column: IEntityAttribute) (view: TypeDeclarations.View) = 
    Main.removeOrdering column view
  static member ChangeWidth (column: IEntityAttribute) width (view: TypeDeclarations.View) = 
    Main.changeWidth column width view
  static member SetFilter (filter: FilterExpression) (view: TypeDeclarations.View) = 
    Main.setFilter filter view
  static member AndFilters (filter: FilterExpression) (view: TypeDeclarations.View) = 
    Main.andFilters filter view
  static member OrFilters (filter: FilterExpression) (view: TypeDeclarations.View) = 
    Main.orFilters filter view
  static member RemoveFilter (view: TypeDeclarations.View) = 
    Main.removeFilter view
  static member AddRelatedColumn (rel: EntityRelationship) (columns: IEntityAttribute list) (columnWidths: int list) (indexes: int list) (view: TypeDeclarations.View) =
    Main.addRelatedColumn rel columns columnWidths indexes view
  static member AddRelatedColumnFirst (rel: EntityRelationship) (columns: IEntityAttribute list) (columnWidths: int list) (view: TypeDeclarations.View) =
    Main.addRelatedColumnFirst rel columns columnWidths view
  static member AddRelatedColumnLast (rel: EntityRelationship) (columns: IEntityAttribute list) (columnWidths: int list) (view: TypeDeclarations.View) =
    Main.addRelatedColumnLast rel columns columnWidths view
  static member RemoveRelatedColumn (rel: EntityRelationship) (view: TypeDeclarations.View) =
    Main.removeLink rel view
  static member ChangeId guid (view: TypeDeclarations.View) =
    Main.changeId guid view
  static member InitFilter (operator: LogicalOperator) = 
    Main.initFilter operator
  static member AddCondition (attributeEntity: EntityAttribute<'a,'b>) (operator: 'b) (arg: 'a) (filter: FilterExpression) =
    Main.addCondition attributeEntity operator arg filter
  static member AddCondition2 (attributeEntity: EntityAttribute<'a,'b>) (operator: 'b) (arg1: 'a) (arg2: 'a) (filter: FilterExpression) =
    Main.addCondition2 attributeEntity operator arg1 arg2 filter
  static member AddConditionMany (attributeEntity: EntityAttribute<'a,'b>) (operator: 'b) (arg: 'a list) (filter: FilterExpression) =
    Main.addConditionMany attributeEntity operator arg 
  static member AddFilter (toAdd: FilterExpression) (filter: FilterExpression) = 
    Main.addFilter toAdd filter
