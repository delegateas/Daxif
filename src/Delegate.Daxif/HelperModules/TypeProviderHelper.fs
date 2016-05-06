namespace DG.Daxif.HelperModules

open System
open DG.Daxif
open DG.Daxif.HelperModules.Common

open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Client

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Quotations

module internal TypeProviderHelper =

  // Class to store data retrieved from Crm lazily
  type XrmData(proxy:Lazy<OrganizationServiceProxy>) =
    member this.entities = 
      lazy
        CrmData.Metadata.allEntities proxy.Value |> List.ofSeq

    member this.entityAttributes =
      lazy 
        this.entities.Value
        |> List.map (fun em ->
          (em.LogicalName,
            lazy
              CrmData.Metadata.entityAll proxy.Value em.LogicalName))
        |> Map.ofList

    member this.records = 
      lazy
        this.entities.Value
        |> List.map (fun em ->
          (em.LogicalName,
            lazy 
              CrmDataInternal.Entities.retrieveAllEntitiesLight
                proxy.Value em.LogicalName))
        |> Map.ofList

    member this.version = lazy CrmDataInternal.Info.version proxy.Value

  // Get relationship information based on what type of relationship it is
  let getRelationshipInfo eName (rel:obj) = 
    match rel with
    | :? OneToManyRelationshipMetadata as rel -> 
      match rel.ReferencingEntity.Equals(eName) with
      | true  -> (rel.SchemaName, rel.ReferencingAttribute)
      | false  -> (rel.SchemaName, rel.ReferencedAttribute)
    | :? ManyToManyRelationshipMetadata as rel -> 
      match rel.Entity1LogicalName.Equals(eName) with
      | true -> (rel.SchemaName, rel.Entity1IntersectAttribute)
      | false -> (rel.SchemaName, rel.Entity2IntersectAttribute)
    | _ -> failwith "Unknown relationship type"


  let createEntityMetadataProps (data:XrmData) (entity:string) () =
    let metadata = data.entityAttributes.Value.[entity].Value

    // Add "static" LogicalName and SchemaName as well as Primary Attributes
    ([ ProvidedProperty(
        "(LogicalName)",
        typeof<string>,
        IsStatic = true,
        GetterCode = fun args ->
          let logicalName = metadata.LogicalName 
          <@@ logicalName @@>);
       ProvidedProperty(
        "(SchemaName)",
        typeof<string>,
        IsStatic = true,
        GetterCode = fun args ->
          let schemaName = metadata.SchemaName
          <@@ schemaName @@>);
       ProvidedProperty(
        "(PrimaryIdAttribute)",
        typeof<string>,
        IsStatic = true,
        GetterCode = fun args ->
          let primaryIdAttribute = metadata.PrimaryIdAttribute
          <@@ primaryIdAttribute @@>);
        ProvidedProperty(
        "(PrimaryNameAttribute)",
        typeof<string>,
        IsStatic = true,
        GetterCode = fun args ->
          let primaryNameAttribute = metadata.PrimaryNameAttribute
          <@@ primaryNameAttribute @@>); ]) @

    // Add static array of all attributes
    ([ ProvidedProperty(
        "(All Attributes)",
        typeof<string array>,
        IsStatic=true,
        GetterCode = fun args -> 
        let all = metadata.Attributes |> Array.map (fun x -> x.LogicalName)
        <@@ all @@>) ]) @

    (metadata.Attributes // Add attribute props
    |> List.ofArray
    |> List.map (fun attr -> 
      ProvidedProperty(attr.SchemaName, typeof<string>, IsStatic=true,
        GetterCode = fun args -> 
          let name = attr.LogicalName
          <@@ name @@>))) @

    // Add relationship props
    ([  metadata.OneToManyRelationships  |> Array.map box
        metadata.ManyToOneRelationships  |> Array.map box
        metadata.ManyToManyRelationships |> Array.map box
     ] 
     |> Array.concat |> List.ofArray 
     |> List.map (getRelationshipInfo metadata.LogicalName)
     |> List.map
      (fun (sname, lname) -> 
        ProvidedProperty(sname, typeof<string>, IsStatic=true,
          GetterCode = fun args -> 
            <@@ lname @@>)
    ))


  let getEntityRecords (data:XrmData) (em:EntityMetadata) () =

    // Add static array of all entities (light)
    let allRecords () =
      ProvidedProperty(
        "(All Records)",
        typeof<Guid array>,
        IsStatic=true,
        GetterCode = fun args -> 
        let all =
          data.records.Value.[em.LogicalName].Value
//          |> Seq.map(fun e -> 
//            e.Attributes.[em.PrimaryNameAttribute].ToString(),e.Id.ToString())
//          |> Seq.map(fun (name,id) -> <@@ Tuple(item1 = (name,Guid(id))) @@>)
          |> Seq.map(fun e -> let id = e.Id.ToString() in <@@ Guid(id) @@>)
          |> Seq.toList
        Expr.NewArray(typedefof<Guid>,all))

    let records = 
      data.records.Value.[em.LogicalName].Value
      |> Seq.choose (fun e ->
        match em.PrimaryNameAttribute with
        | null -> None
        | nameAttr ->
          match box (e.GetAttributeValue nameAttr) with
          | :? String as v when not(String.IsNullOrWhiteSpace v) ->
            ProvidedProperty(v, typeof<Guid>, IsStatic=true,
              GetterCode = fun args -> 
                let id = e.Id.ToString()
                <@@ Guid(id) @@>) |> Some
          | _ -> None)
      |> List.ofSeq

    match records.IsEmpty with
    | false -> allRecords () :: records
    | true  -> 
      [ProvidedProperty("No records available", typeof<obj>, IsStatic=true,
        GetterCode = fun args -> <@@ null @@>)]


  let createEntityMetadataType (data:XrmData) (entity:EntityMetadata) =
    let ty = ProvidedTypeDefinition(entity.SchemaName, Some typeof<obj>)
    ty.AddXmlDocDelayed(fun () -> sprintf "Entity metadata for '%s'" entity.LogicalName)
    ty.AddMembersDelayed(createEntityMetadataProps data entity.LogicalName)
    (entity.LogicalName, ty)

  let createEntityRecordType (data:XrmData) (em:EntityMetadata) =
    let ty = ProvidedTypeDefinition(em.SchemaName, Some typeof<obj>)
    ty.AddXmlDocDelayed(fun () -> sprintf "Entity records for '%s'" em.LogicalName)
    ty.AddMembersDelayed(getEntityRecords data em)
    (em.LogicalName, ty)



  let generateTypeProvider (ty:ProvidedTypeDefinition) 
    (proxy:Lazy<OrganizationServiceProxy>) =

    let data = XrmData proxy

    let version =
      ProvidedProperty("Version", typeof<Tuple<string,CrmReleases>>, IsStatic=true,
        GetterCode = fun args -> 
          let version,release = data.version.Value
          <@@ (version,release) @@>)
    version.AddXmlDocDelayed(
      fun () -> "Returns the version number of the CRM system.")

    

    // Setup metadata types
    let metadata =
      ProvidedTypeDefinition("Metadata", baseType=Some typeof<obj>,
        HideObjectMethods = true)

    let entityMetadataTypes = 
      lazy
        data.entities.Value 
        |> List.map (createEntityMetadataType data)
        |> Map.ofList

    metadata.AddXmlDocDelayed(
      fun () -> "Access the metadata of the CRM system.")
    metadata.AddMemberDelayed(
      fun () -> 
        // Add static array of all entities
        ProvidedProperty(
            "(All Entities)",
            typeof<string array>,
            IsStatic=true,
            GetterCode = fun args -> 
            let all = data.entities.Value
                      |> Array.ofList
                      |> Array.map (fun x -> x.LogicalName)
            <@@ all @@>))
    metadata.AddMembersDelayed(
      fun () -> entityMetadataTypes.Value |> Map.toList |> List.map snd)


    // Setup records
    let records =
      ProvidedTypeDefinition("Records", baseType=Some typeof<obj>,
        HideObjectMethods = true)

    let entityRecordTypes = 
      lazy
        data.entities.Value 
        |> List.map (createEntityRecordType data)
        |> Map.ofList

    records.AddXmlDocDelayed(
      fun () -> "Access the records of the CRM system.")
    records.AddMembersDelayed(
      fun () -> entityRecordTypes.Value |> Map.toList |> List.map snd)


    // Add members to super type
    ty.AddMember version
    ty.AddMemberDelayed (fun () -> metadata)
    ty.AddMemberDelayed (fun () -> records)

    ty
