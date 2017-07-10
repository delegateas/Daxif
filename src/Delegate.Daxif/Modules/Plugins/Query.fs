module DG.Daxif.Modules.Plugin.Query

open System
open Microsoft.Xrm.Sdk.Query

/// Create a query to get a plugin assembly by its name
let pluginAssemblyByName (name: string) = 
  let q = QueryExpression("pluginassembly")
  q.ColumnSet <- ColumnSet("pluginassemblyid", "name", "sourcehash")

  let f = FilterExpression()
  f.AddCondition(ConditionExpression("name", ConditionOperator.Equal, name))
  q.Criteria <- f

  let le = LinkEntity()
  le.JoinOperator <- JoinOperator.LeftOuter
  le.LinkFromAttributeName <- @"pluginassemblyid"
  le.LinkFromEntityName <- @"pluginassembly"
  le.LinkToAttributeName <- @"objectid"
  le.LinkToEntityName <- @"solutioncomponent"
  le.Columns.AddColumn("solutionid")
  q.LinkEntities.Add(le)
  q
    
/// Create a query to get plugin assemblies by solution
let pluginAssembliesBySolution (solutionId: Guid) = 
  let q = QueryExpression("pluginassembly")
  q.ColumnSet <- ColumnSet("pluginassemblyid", "name", "sourcehash", "isolationmode")

  let le = LinkEntity()
  le.JoinOperator <- JoinOperator.Inner
  le.LinkFromAttributeName <- @"pluginassemblyid"
  le.LinkFromEntityName <- @"pluginassembly"
  le.LinkToAttributeName <- @"objectid"
  le.LinkToEntityName <- @"solutioncomponent"
  le.LinkCriteria.Conditions.Add(ConditionExpression("solutionid", ConditionOperator.Equal, solutionId))
  q.LinkEntities.Add(le)
  q
    
/// Create a query to get plugin types by assembly
let pluginTypesByAssembly (assemblyId: Guid) = 
  let q = QueryExpression("plugintype")
  q.ColumnSet <- ColumnSet(true)

  let f = FilterExpression()
  f.AddCondition(ConditionExpression("pluginassemblyid", ConditionOperator.Equal, assemblyId))
  q.Criteria <- f
  q

/// Create a query to get plugin types by solution
let pluginTypesBySolution (solutionid: Guid) = 
  let q = QueryExpression("plugintype")
  q.ColumnSet <- ColumnSet(true)

  let f = FilterExpression()
  f.AddCondition(ConditionExpression("solutionid", ConditionOperator.Equal, solutionid))
  q.Criteria <- f
  q

/// Create a query to get plugin steps by solution
let pluginStepsBySolution (solutionId: Guid) = 
  let q = QueryExpression("sdkmessageprocessingstep")
  q.ColumnSet <- ColumnSet(true)
    
  let le = LinkEntity()
  le.JoinOperator <- JoinOperator.Inner
  le.LinkFromAttributeName <- @"sdkmessageprocessingstepid"
  le.LinkFromEntityName <- @"sdkmessageprocessingstep"
  le.LinkToAttributeName <- @"objectid"
  le.LinkToEntityName <- @"solutioncomponent"
  le.LinkCriteria.Conditions.Add(ConditionExpression("solutionid", ConditionOperator.Equal, solutionId))
  q.LinkEntities.Add(le)
  q

/// Create a query to get plugin steps by type
let pluginStepsByType (typeId: Guid) = 
  let q = QueryExpression("sdkmessageprocessingstep")
  q.ColumnSet <- ColumnSet(true)

  let f = FilterExpression()
  f.AddCondition(ConditionExpression("plugintypeid", ConditionOperator.Equal, typeId))
  q.Criteria <- f
  q
    
/// Create a query to get plugin step images by step
let pluginStepImagesByStep (stepId: Guid) = 
  let q = QueryExpression("sdkmessageprocessingstepimage")
  q.ColumnSet <- ColumnSet(true)

  let f = FilterExpression()
  f.AddCondition(ConditionExpression("sdkmessageprocessingstepid", ConditionOperator.Equal, stepId))
  q.Criteria <- f
  q

/// Create a query to get plugin step images by solution
let pluginStepImagesBySolution (solutionId: Guid) = 
  let q = QueryExpression("sdkmessageprocessingstepimage")
  q.ColumnSet <- ColumnSet(true)

  let f = FilterExpression()
  f.AddCondition(ConditionExpression("solutionid", ConditionOperator.Equal, solutionId))
  q.Criteria <- f
  q

/// Create a query to get a SdkMessage from its name (event operation)
let sdkMessage (eventOperation: string) = 
  let q = QueryExpression("sdkmessage")
  q.ColumnSet <- ColumnSet("sdkmessageid", "name")
  q.TopCount <- Nullable(1)

  let f = FilterExpression()
  f.AddCondition(ConditionExpression("name", ConditionOperator.Equal, eventOperation))
  q.Criteria <- f
  q

/// Create a query to get a SdkMessageFilter from its parent message and entity type
let sdkMessageFilter (primaryObjectType: string) (sdkMessageId: Guid) = 
  let q = QueryExpression("sdkmessagefilter")
  q.ColumnSet <- ColumnSet("sdkmessagefilterid")

  let f = FilterExpression()
  f.AddCondition(ConditionExpression(@"sdkmessageid", ConditionOperator.Equal, sdkMessageId))
  match String.IsNullOrEmpty(primaryObjectType) with
  | true -> ()
  | false -> 
    f.AddCondition(ConditionExpression(@"primaryobjecttypecode", ConditionOperator.Equal, primaryObjectType))
  q.Criteria <- f
  q 