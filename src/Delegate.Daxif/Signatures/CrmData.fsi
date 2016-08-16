(*** hide ***)
namespace DG.Daxif.HelperModules.Common

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Query
open DG.Daxif

/// Implements functions to get data and metadata from CRM
module CrmData = 
(**
CrmData
==========

Retrieves and outputs the CRM version to the log.
*)
  module Metadata = 
(**
Metadata
========

TODO:
*)
    /// TODO:
    val public entity : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> EntityMetadata

    /// TODO:
    val public entityAll : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> EntityMetadata

    /// TODO:
    val public entityAttributes : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> AttributeMetadata array

    /// TODO:
    val public entityOneToManyRelationships : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> OneToManyRelationshipMetadata array

    /// TODO:
    val public entityManyToOneRelationships : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> OneToManyRelationshipMetadata array

    /// TODO:
    val public entityManyToManyRelationships : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> ManyToManyRelationshipMetadata array

    /// TODO:
    val public allEntities : proxy:OrganizationServiceProxy
       -> EntityMetadata seq

  module CRUD = 
(**
CRUD
====

TODO:
*)

    /// TODO:
    val public version : unit
       -> unit

    /// TODO:
    val public createReq : entity:Entity
       -> parameters:ParameterCollection
       -> CreateRequest

    /// TODO:
    val public create : proxy:OrganizationServiceProxy
       -> entity:Entity
       -> parameters:ParameterCollection
       -> Guid

    /// TODO:
    val public retrieveReq : logicalName:string
       -> guid:Guid
       -> RetrieveRequest

    /// TODO:
    val public retrieve : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> guid:Guid
       -> Entity

    /// TODO:
    val public retrieveMultiple : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> query:QueryExpression
       -> Entity seq

    /// TODO:
    val public updateReq : entity:Entity
       -> UpdateRequest

    /// TODO:
    val public update : proxy:OrganizationServiceProxy
       -> entity:Entity
       -> Guid

    /// TODO:
    val public deleteReq : logicalName:string
       -> guid:Guid
       -> DeleteRequest

    /// TODO:
    val public delete : proxy:OrganizationServiceProxy
       -> logicalName:string
       -> guid:Guid
       -> Guid

    /// TODO:
    val public publish : proxy:OrganizationServiceProxy
       -> unit

    val public performAsBulk : proxy:OrganizationServiceProxy
      -> reqs:OrganizationRequest []
      -> ExecuteMultipleResponseItem []

    val public performAsBulkWithOutput : proxy:OrganizationServiceProxy
      -> logLevel:LogLevel
      -> reqs:OrganizationRequest []      
      -> unit