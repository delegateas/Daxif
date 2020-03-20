module Delegate.Daxif.Test.Example

open Xunit
open System
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Query
open DG.Daxif
open DG.Daxif.Modules.Solution

//type testEnvironment = 

type TestOrg () = 

  interface IOrganizationService with
    member this.Associate(entityName: string, entityId: Guid, relationship: Relationship, relatedEntities: EntityReferenceCollection): unit = 
      raise (System.NotImplementedException())
    member this.Create(entity: Entity): Guid = 
      raise (System.NotImplementedException())
    member this.Delete(entityName: string, id: Guid): unit = 
      raise (System.NotImplementedException())
    member this.Disassociate(entityName: string, entityId: Guid, relationship: Relationship, relatedEntities: EntityReferenceCollection): unit = 
      raise (System.NotImplementedException())
    member this.Retrieve(entityName: string, id: Guid, columnSet: ColumnSet): Entity = 
      raise (System.NotImplementedException())
    member this.RetrieveMultiple(query: QueryBase): EntityCollection = 
      raise (System.NotImplementedException())
    member this.Update(entity: Entity): unit = 
      raise (System.NotImplementedException())
    member this.Execute( req: OrganizationRequest ) = 
      match req with
      | :? UpdateRequest -> 
        printfn "test"
        UpdateResponse() :> OrganizationResponse
      | :? Messages.WhoAmIRequest -> 
        let resp = Messages.WhoAmIResponse()
        resp.Results.Add("UserId", Guid.NewGuid())
        Messages.WhoAmIResponse() :> OrganizationResponse
      | _ -> 
        raise (System.NotImplementedException())

let service = TestOrg() :> IOrganizationService

//[<Fact>]
//let GetStatusRequest() =
//  Solution.Import()
//    //let id = whoAmI service
//    Assert.NotNull id