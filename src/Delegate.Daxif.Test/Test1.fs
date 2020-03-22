module Delegate.Daxif.Test.Basic

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
open Microsoft
open DG.Daxif

type TestOrg () = 
  [<DefaultValue>] val mutable published : bool
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
      | :? Messages.PublishAllXmlRequest -> 
        this.published <- true
        printfn "Published All"
        Messages.PublishAllXmlResponse() :> OrganizationResponse
      | :? UpdateRequest -> 
        printfn "test"
        UpdateResponse() :> OrganizationResponse
      | :? Messages.WhoAmIRequest -> 
        let resp = Messages.WhoAmIResponse()
        resp.Results.Add("UserId", Guid.NewGuid())
        Messages.WhoAmIResponse() :> OrganizationResponse
      | _ -> 
        raise (System.NotImplementedException())



[<Fact>]
let TestWhoAmI() =
  let mockEnv = EnvironmentMock.CreateEnvironment (TestOrg() :> IOrganizationService)
  let newService = mockEnv.connect().GetService()
  let req = Messages.WhoAmIRequest()
  let resp = newService.Execute(req) :?> Messages.WhoAmIResponse
  let id = resp.UserId
  Assert.NotNull id

[<Fact>]
let TestPublishAll() =
  let testOrg = TestOrg()
  Assert.False testOrg.published
  let mockEnv = EnvironmentMock.CreateEnvironment testOrg
  Solution.PublishCustomization(mockEnv)
  Assert.True testOrg.published


