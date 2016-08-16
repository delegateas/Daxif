(**
DataInMemoryMigrate
=================

Libraries
---------

Config information *)
#load @"DG.XrmOrg.XrmSolution.Config.fsx"

module cfg = DG.XrmOrg.XrmSolution.Config

(** Open libraries for use *)
#r @"System.ServiceModel"

open System
open System.ServiceModel

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Query
 
open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common

(** DAXIF# operations
---------

 Helper functions *)

// Create SDK proxy from an SDK Client
let createProxy usr pwd wsdl' = 
  let auth = new AuthenticationCredentials()
  do auth.ClientCredentials.UserName.UserName <- usr
  do auth.ClientCredentials.UserName.Password <- pwd
  let manager =
    ServiceConfigurationFactory.CreateManagement<IOrganizationService>
      (wsdl')
  let token = manager.Authenticate(auth)
  
  let proxy () = 
    let proxy' = new OrganizationServiceProxy(manager, token.SecurityTokenResponse)
    do proxy'.EnableProxyTypes()
    proxy'
  proxy

// Checks if a entity exist
let exist proxy logicalName guid primaryattribute =
  let (proxy : OrganizationServiceProxy) = proxy
  let (guid : Guid) = guid
      
  let an = // Limit the amount of network calls
    match primaryattribute with
    | Some v -> v
    | None -> 
      let em = CrmData.Metadata.entity proxy logicalName
      em.PrimaryIdAttribute
      
  let f = FilterExpression()
  f.AddCondition(ConditionExpression(an, ConditionOperator.Equal, guid))
  let q = QueryExpression(logicalName)
  q.ColumnSet <- ColumnSet(an)
  q.Criteria <- f
  q.NoLock <- true
  CrmData.CRUD.retrieveMultiple proxy logicalName q |> Seq.length
  > 0

// Creates a retrieve request for an entity
let createRetrieveEntitieReq logicName =
  let req = RetrieveMultipleRequest()
  let query = QueryExpression(logicName)
  query.ColumnSet <- ColumnSet(true)
  query.Criteria <- FilterExpression()
  req.Query <- query
  req :> OrganizationRequest


(** Define Typeprovider from source and create SDK proxy for source and target *)
type source = XrmProvider<uri = cfg.wsdlSource, usr = cfg.usrSource, 
                          pwd = cfg.pwdSource, domain = cfg.domainSource, 
                          ap = cfg.authType>

// Create proxy for dev and test
let proxySource = createProxy cfg.usrSource cfg.pwdSource cfg.wsdlSource'
let proxyTarget = createProxy cfg.usrTarget cfg.pwdTarget cfg.wsdlTarget'

(** Define entities to be exported *)
let entities = 
  [|
    source.Metadata.Account.``(LogicalName)``;
    source.Metadata.Contact.``(LogicalName)``
  |]

(** Fetch Entities from source *)
let entities' = 
  entities
  |> Array.map createRetrieveEntitieReq
  |> CrmData.CRUD.performAsBulk (proxySource())
  |> fun x -> 
    x
    |> Array.iter(fun r -> 
      if r.Fault <> null then 
        eprintfn "Error when performing %s: %s" (x.[r.RequestIndex].ToString()) r.Fault.Message)
    x
  |> Array.filter(fun x -> x.Fault = null)
  |> Array.map(fun x -> 
    (x.Response :?> RetrieveMultipleResponse).EntityCollection.Entities)
  |> Seq.concat

printfn "Entities to update/create: %i" (Seq.length entities')


(** Resolve missing Entity References in entities *)
let entities''=
  entities'
  |> Seq.map(fun e -> 
    let a'' = 
      e.Attributes 
      |> Seq.fold(fun (a:AttributeCollection) x -> 
        match x.Value with
        | :? EntityReference as er ->
          let p = proxyTarget()
          let ln = er.LogicalName
          let em = CrmData.Metadata.entity p ln
          let id = er.Id
          
          let bool = exist p ln id (em.PrimaryIdAttribute |> Some)
          
          match bool with
          | false -> 
              printfn "%s:%s doesn't exist in target" ln (id.ToString())
          | true -> a.Add(x.Key, new EntityReference(ln, id))
        | :? EntityCollection | _ -> a.Add(x)

        a) (new AttributeCollection())
                    
    e.Attributes.Clear()
    e.Attributes.AddRange(a'')
    e)

(** Update entities existing in target and create new entities for those that 
   does not exist in target *)
entities''
|> Seq.map(fun e -> 
  let p = proxyTarget()
  let em = CrmData.Metadata.entity p e.LogicalName
  let bool = exist p e.LogicalName e.Id (em.PrimaryIdAttribute |> Some)
  match bool with
  | true ->
    CrmData.CRUD.updateReq e :> OrganizationRequest
  | false -> 
    CrmData.CRUD.createReq e (ParameterCollection()) :> OrganizationRequest )      

|> Seq.toArray
|> CrmData.CRUD.performAsBulk (proxyTarget())
|> fun x -> 
    x
    |> Array.iter(fun r -> 
      if r.Fault <> null then 
        eprintfn "Error when performing %s: %s" (x.[r.RequestIndex].ToString()) r.Fault.Message)
    x
|> Array.filter (fun x -> x.Fault = null)
|> Array.length
|> fun count -> 
  printfn "Succesfully performed %d/%d" count (Seq.length entities')
 


