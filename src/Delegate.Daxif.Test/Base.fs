module Delegate.Daxif.Test.OrganizationMock

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Query

type internal BaseMockOrg () = 
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
      raise (System.NotImplementedException())
