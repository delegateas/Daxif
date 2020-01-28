﻿module internal DG.Daxif.Modules.View.Generator

open System
open System.IO
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Query
open System.Text.RegularExpressions
open System.Globalization
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open Microsoft.Xrm.Sdk.Metadata
open TypeDeclarations
open DG.Daxif

let writeViewGuidFile proxy (sw : StreamWriter) (entityLogicalName : string) = 
  let uppercaseLogicalName = entityLogicalName.Substring(0, 1).ToUpper() + entityLogicalName.Substring(1)
  sw.WriteLine("  module " + uppercaseLogicalName + " =")
  let query = QueryExpression(ViewLogicalName)
  query.ColumnSet <- ColumnSet("name")
  query.Criteria <- FilterExpression()
  query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entityLogicalName)
  query.Criteria.AddCondition("fetchxml", ConditionOperator.NotNull)
  query.Criteria.AddCondition("layoutxml", ConditionOperator.NotNull)
  query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0)
  query.Criteria.AddCondition("querytype", ConditionOperator.In, 0, 1, 2, 4, 64) // Desired Views https://msdn.microsoft.com/en-us/library/microsoft.crm.sdk.savedqueryquerytype.aspx
  CrmData.CRUD.retrieveMultiple proxy ViewLogicalName query
  |> Seq.map (fun (e : Entity) -> ((string) e.Attributes.["name"], (string) e.Id))
  |> Seq.fold (fun previousNames (name, guid) ->
    let regex = new Regex(@"[^a-zA-Z0-9_]")
    let trimmedName = 
      regex.Replace(name, "")
      |> fun s -> if Char.IsNumber(s, 0) then "_" + s else s

    let duplicates, nextMap = 
      match Map.tryFind trimmedName previousNames with
      | Some i -> i, Map.add trimmedName (i + 1) previousNames
      | None -> 0, Map.add trimmedName 1 previousNames
    
    let safeName = if duplicates > 0 then trimmedName + duplicates.ToString() else trimmedName
    sw.WriteLine("    let " + safeName + " = Guid(\"" + guid + "\")")
    nextMap) Map.empty
  |> ignore

let generateGuidFile proxy (daxifRoot: string) (entityLogicalNames : string list) = 
  use sw = new StreamWriter(daxifRoot ++ generationFolder ++ "_ViewGuids.fsx")
  let startTag = [ "namespace ViewGuids"; "open System"; "module Views =" ] |> String.concat "\n"
  sw.WriteLine(startTag)
  List.iter (fun e -> writeViewGuidFile proxy sw e) (entityLogicalNames)

let writeRelationFile proxy (sw : StreamWriter) (entityLogicalName : string) = 
  let upCase s = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s)
  let relations = CrmData.Metadata.entityManyToOneRelationships proxy entityLogicalName
  if relations |> Array.isEmpty |> not then
    sw.WriteLine("module " + upCase entityLogicalName + " =")
    sw.WriteLine("  module Relations =")
    relations
    |> Array.map (fun (rel : OneToManyRelationshipMetadata) -> 
      (rel.ReferencedEntity, rel.ReferencedAttribute, rel.ReferencingEntity, rel.ReferencingAttribute))
    |> Array.iter (fun (refedEnt, refedAttr, refingEnt, refingAttr) ->
      sw.WriteLine(
        ["    let ";
        upCase refingAttr;
        "_";
        upCase refedEnt;
        upCase refedAttr;
        " = EntityRelationship.Rel(\"";
        refedEnt;
        "\", \"";
        refedAttr;
        "\", \"";
        refingEnt;
        "\", \"";
        refingAttr;
        "\")"]
        |> String.concat ""))
    
let generateEntRelFile proxy (daxifRoot: string) (entityLogicalNames : string list) = 
  use sw = new StreamWriter(daxifRoot ++ generationFolder ++ "_EntityRelationships.fsx")
  let startTag = 
    [ "namespace EntityRelationships"; 
      "#r @\"" + (daxifRoot ++ "bin" ++ "Delegate.Daxif.dll") + "\"";
      "open DG.Daxif.Modules.View.TypeDeclarations"] 
    |> String.concat "\n"
  sw.WriteLine(startTag)
  List.iter (fun e -> writeRelationFile proxy sw e) (entityLogicalNames)

let upCase s = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s)

let writeOptionSet (sw : StreamWriter) (optionSet : OptionSetMetadata) logicalname =
  if optionSet.Options.Count <> 0 then
    sw.WriteLine("  type " + upCase logicalname + "Opt = ")
    optionSet.Options
    |> Seq.fold (fun previousNames e ->
    let label = e.Label.UserLocalizedLabel.Label
    let safeLabel = if label = "" then "__EMPTYLABEL__" else label
    let duplicates, nextMap = 
      match Map.tryFind safeLabel previousNames with
      | Some i -> i, Map.add safeLabel (i + 1) previousNames
      | None -> 0, Map.add safeLabel 1 previousNames
    
    let safeName = if duplicates > 0 then label + duplicates.ToString() else label
    sw.WriteLine("    | " + "``" + safeName + "``" + " = " + string e.Value.Value)
    nextMap) Map.empty
  |> ignore

let fitMetadata (sw : StreamWriter) (metadata : AttributeMetadata[]) options =
  metadata
  |> Array.fold (fun (options, fitted) (meta : AttributeMetadata) -> 
    match meta with
    | :? EnumAttributeMetadata as eam  ->
        if eam.OptionSet = null 
          || Set.contains meta.LogicalName options
          || (eam.OptionSet <> null && eam.OptionSet.Options.Count = 0) then 
          options, 
            (meta.LogicalName, (Map.find (meta.AttributeType.Value.ToString()) AttributeTypes.convertionMap)) :: fitted
        else
          writeOptionSet sw eam.OptionSet meta.LogicalName
          (Set.add meta.LogicalName options), 
            (meta.LogicalName, ((upCase meta.LogicalName + "Opt"),"OptionCondition")) :: fitted
        
    | _ ->
      options, 
        (meta.LogicalName, (Map.find (meta.AttributeType.Value.ToString()) AttributeTypes.convertionMap)) :: fitted
    ) (options, []) 


let writeEntity proxy (sw : StreamWriter) (swOptionSet : StreamWriter) (entityLogicalName : string) 
  (options : Set<string>) = 
  sw.WriteLine("module " + upCase entityLogicalName + " =")
  sw.WriteLine("  module Fields =")
  let entMetadata = CrmData.Metadata.entityAttributes proxy entityLogicalName
  let options', fittedData = fitMetadata swOptionSet entMetadata options
  fittedData
  |> List.iter (fun (logicalname, (attrType, condType)) ->
      sw.WriteLine(
        ["    let ";
        upCase logicalname;
        " = EntityAttribute<";
        attrType;
        ", ";
        condType;
        ">(\"";
        logicalname;
        "\")"]
        |> String.concat "")) 
  options'   

let generateEntAttrFile proxy (daxifRoot: string) (entityLogicalNames : string list) = 
  use sw = new StreamWriter(daxifRoot ++ generationFolder ++ "_EntityAttributes.fsx")
  let startTag = 
    [ "namespace EntityAttributes"; 
    "#r @\"" + (daxifRoot ++ "bin" ++ "Delegate.Daxif.dll") + "\"";
    "#r @\"" + (daxifRoot ++ "bin" ++ "Microsoft.Xrm.Sdk.dll") + "\"";
    "#load @\"_OptionSetTypes.fsx\"";
    "open DG.Daxif.Modules.View.TypeDeclarations";
    "open DG.Daxif.Modules.View.AllowedConditions";
    "open OptionSetTypes";
    "open Microsoft.Xrm.Sdk"]
    |> String.concat "\n"
  sw.WriteLine(startTag)
  use swOptionSet = new StreamWriter(daxifRoot ++ generationFolder ++ "_OptionSetTypes.fsx")
  swOptionSet.WriteLine(["namespace OptionSetTypes"] |> String.concat "\n")
  entityLogicalNames
  |> List.fold (fun options e -> writeEntity proxy sw swOptionSet e options) Set.empty
  |> ignore

let getFullEntityList entities solutions proxy =
  log.Verbose "Figuring out which entities should be included.."
  let solutionEntities = 
    match solutions with
    | Some sols -> 
      sols 
      |> Array.map (CrmDataInternal.Entities.retrieveSolutionEntities proxy)
      |> Seq.concat |> Set.ofSeq
    | None -> Set.empty

  let finalEntities =
    match entities with
    | Some ents -> Set.union solutionEntities (Set.ofArray ents)
    | None -> solutionEntities
  
  finalEntities |> Set.toList

let generateFiles proxyGen daxifRoot entities solutions =
  let p = proxyGen()
  log.Verbose "Getting entities from solution and config"
  let allEntities = getFullEntityList entities solutions p
  log.Verbose "Generating files for '%s' entities" (string allEntities.Length)
  log.Verbose "Generating entity relationship file"
  generateEntRelFile p daxifRoot allEntities
  log.Verbose "Generating entity attribute file"
  generateEntAttrFile p daxifRoot allEntities
  log.Verbose "Generating view guid file"
  generateGuidFile p daxifRoot allEntities
  log.Verbose "Generation done"