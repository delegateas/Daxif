(**
MetadataUnitTests.fsx
===========================

Load all libraries and .fs/.fsx files *)
//#load @"__temp__.fsx"
//#load @"AuthInfo.fsx"

(** Open libraries for use *)
open System
open System.IO
open System.Runtime.Serialization
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Query
open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common

(**
Unittest setUp
==============

Setup of shared values *)

let usr = AuthInfo.usr
let pwd =  AuthInfo.pwd
let domain = AuthInfo.domain
let ap = AuthenticationProviderType.OnlineFederation
let ac = Authentication.getCredentials ap usr pwd domain
let wsdl = AuthInfo.wsdl
let wsdl' = wsdl

(** Instantiate service manager and service proxy *)
let m = ServiceManager.createOrgService wsdl
let tc = m.Authenticate(ac)
let p = ServiceProxy.getOrganizationServiceProxy m tc

//let m' = ServiceManager.createOrgService wsdl'
//let tc' = m.Authenticate(ac)
//let p' = ServiceProxy.getOrganizationServiceProxy m' tc'
//
//let root = __SOURCE_DIRECTORY__
//let tmp = Path.Combine(root, "tmp")
//let tmp' = Path.Combine(root, "tmp_")
//
//let ensureNoFile path =
//    match File.Exists(path) with
//    | true -> File.Delete(path)
//    | false -> ()
//
//let ensureFolder path = 
//    match Directory.Exists(path) with
//    | true -> ()
//    | false -> Directory.CreateDirectory(path) |> ignore
//
//CrmData.Metadata.allEntities p
////|> Seq.filter(fun x -> x.IsCustomEntity = Nullable(false))
//|> Seq.toArray
//|> Array.Parallel.iter(fun x -> 
//    let p'' = ServiceProxy.getOrganizationServiceProxy m tc    
//    CrmData.Metadata.entityAttributes p'' x.LogicalName
//    |> Array.map(fun y -> y.SchemaName,SerializationHelper.serializeObjectToBytes Serialize.XML y)
//    |> Array.iter(fun (y,ys) -> 
//        let tmp'' = Path.Combine(tmp,x.SchemaName)
//        tmp'' |> ensureFolder
//        File.WriteAllBytes(Path.Combine(tmp'',y + ".xml"),ys)))
//
//CrmData.Metadata.allEntities p'
////|> Seq.filter(fun x -> x.IsCustomEntity = Nullable(false))
//|> Seq.toArray
//|> Array.Parallel.iter(fun x -> 
//    let p'' = ServiceProxy.getOrganizationServiceProxy m' tc'  
//    CrmData.Metadata.entityAttributes p'' x.LogicalName
//    |> Array.map(fun y -> y.SchemaName,SerializationHelper.serializeObjectToBytes Serialize.XML y)
//    |> Array.iter(fun (y,ys) -> 
//        let tmp'' = Path.Combine(tmp',x.SchemaName)
//        tmp'' |> ensureFolder
//        File.WriteAllBytes(Path.Combine(tmp'',y + ".xml"),ys)))
//
//Directory.EnumerateFiles(tmp, @"*.xml", SearchOption.AllDirectories)
//|> Seq.toArray
//|> Array.Parallel.map(fun x -> 
//    let y = x.Replace(@"\tmp\",@"\tmp_\")
//    let xs = File.ReadAllBytes(x)
//    let ys = File.ReadAllBytes(y)
//    x,(xs |> Utility.fnv1aHash') = (ys |> Utility.fnv1aHash'))
//|> Array.filter(fun (x,y) -> not y)
//|> Array.fold(fun a (x,y) -> a |> Map.add(x) y) Map.empty
//|> fun x -> SerializationHelper.serializeObjectToBytes Serialize.XML x
//|> fun xs ->
//    let md = Path.Combine(tmp,"metadataDiff.xml")
//    md |> ensureNoFile
//    File.WriteAllBytes(Path.Combine(root,"metadataDiff.xml"),xs)

//CrmData.Metadata.allEntities p
////|> Seq.filter(fun x -> x.IsCustomEntity = Nullable(false))
//|> Seq.fold(fun a x -> 
//    CrmData.Metadata.entityAttributes p x.LogicalName
//    |> Array.map(fun y -> y.SchemaName,SerializationHelper.serializeObjectToBytes SerializeType.XML y)
//    |> Array.map(fun (y,ys) -> y,ys |> Utility.fnv1aHash')
//    |> fun xs -> a |> Map.add(x.SchemaName) xs) Map.empty

(**
Test cases
==============

Retrieve all entities metadata. At least system entities (can't be deleted) exits *)
let tc1 () =
    CrmData.Metadata.allEntities p |> Seq.length > 0

(** Retrieve the "account" entity metadata. Schemas name is "Account" *)
let tc2 () =
    (CrmData.Metadata.entity p @"account").SchemaName.Equals(@"Account")

(** Retrieve the "account" attributes metadata. There is at least the property "name" *)
let tc3 () =
    CrmData.Metadata.entityAttributes p @"account"
    |> Array.filter(fun x -> x.LogicalName = @"name")
    |> Array.length = 1

(** Retrieve the "account" 1-to-N metadata. There is at least an "account-parent-account" relation *)
let tc4 () =
    CrmData.Metadata.entityOneToManyRelationships p @"account"
    |> Array.filter(fun x ->
       x.ReferencingAttribute = @"parentaccountid" &&
       x.ReferencingEntity = @"account" &&
       x.ReferencedAttribute = @"accountid" &&
       x.ReferencedEntity = @"account")
    |> Array.length = 1

(** Retrieve the "account" N-to-1 metadata. There is at least an "account-parent-account" relation *)
let tc5 () =
    CrmData.Metadata.entityManyToOneRelationships p @"account"
    |> Array.filter(fun x ->
       x.ReferencingAttribute = @"parentaccountid" &&
       x.ReferencingEntity = @"account" &&
       x.ReferencedAttribute = @"accountid" &&
       x.ReferencedEntity = @"account")
    |> Array.length = 1

(** Retrieve the "account" N-to-N metadata. There is at least an "account-lead" relation *)
let tc6 () =
    CrmData.Metadata.entityManyToManyRelationships p @"account"
    |> Array.filter(fun x ->
       x.Entity1LogicalName = @"account" &&
       x.Entity2LogicalName = @"lead")
    |> Array.length = 1

(**
Run test cases
==============

Place test cases result in a list *)
let unitTest = [| tc1; tc2; tc3; tc4; tc5; tc6; |]
