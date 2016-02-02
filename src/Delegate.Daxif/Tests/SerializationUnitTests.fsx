(**
SolutionUnitTests.fsx
===========================

Load all libraries and .fs/.fsx files *)
//#load @"__temp__.fsx"
//#load @"AuthInfo.fsx"

open System
open Microsoft.Xrm.Sdk
open DG.Daxif.HelperModules



(**
Test cases
==============
Create a sample of entities -> Serialize -> Deserialize -> equal to the origial entity
 *)
let equalNameAndIdProperty (e1:Entity) (e2:Entity) =
  e1.LogicalName = e2.LogicalName && e1.Id = e2.Id


let testEntities = [ Entity("entity1", new Guid()); Entity("entity2", new Guid())]


 (* We serliaze two entities to binary and the deserialize and check if they are still the same *)
let tc1() =
  testEntities
  |> List.map SerializationHelper.serializeBinary'<Entity>
  |> List.map SerializationHelper.deserializeBinary'<Entity>
  |> List.forall2( fun e1 e2 -> equalNameAndIdProperty e1 e2) testEntities

(* We serliaze two entities to XML and the deserialize and check if they are still the same *)
let tc2() =
  testEntities
  |> List.map SerializationHelper.serializeXML<Entity>
  |> List.map SerializationHelper.toString
  |> List.map SerializationHelper.deserializeXML<Entity>
  |> List.forall2( fun e1 e2 -> equalNameAndIdProperty e1 e2) testEntities

(* We serliaze two entities to Json and the deserialize and check if they are still the same *)
let tc3() =
  testEntities
  |> List.map SerializationHelper.serializeJson<Entity>
  |> List.map SerializationHelper.toString
  |> List.map SerializationHelper.deserializeJson<Entity>
  |> List.forall2( fun e1 e2 -> equalNameAndIdProperty e1 e2) testEntities
(**
Run test cases
==============

Place test cases result in a list *)
let unitTest = [| tc2; tc3;|]