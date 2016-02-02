(**
DataUnitTests.fsx
=======================

Load all libraries and .fs/.fsx files *)
//#load @"__temp__.fsx"
//#load @"AuthInfo.fsx"

(** Open libraries for use *)
open System
open Microsoft.Xrm.Sdk.Client
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

(** Instantiate service manager and service proxy *)
let m = ServiceManager.createOrgService wsdl
let tc = m.Authenticate(ac)
let p = ServiceProxy.getOrganizationServiceProxy m tc

(**
Test cases
==============

We search for a known contact in CRM "7BF52781-3DF5-E311-93FD-00155D56700D" *)
let tc1 () =
    CrmData.Entities.existCrm p @"account"
        (Guid(@"a9327117-ab34-e511-80d1-3863bb34b7b0")) None

(** We search for a known contact in CRM "DFF52781-3DF5-E311-93FD-00155D56700D" *)
let tc2 () =
    CrmData.Entities.existCrm p @"contact"
        (Guid(@"0f337117-ab34-e511-80d1-3863bb34b7b0")) None

(** As we installed the Sample data, we know there are at least 10 contacts *)
let tc3 () =
    CrmData.Entities.retrieveAllEntities p @"contact"
    |> Seq.length > 0

(** As we installed the Sample data, we know there are at least 10 accounts *)
let tc4 () =
    CrmData.Entities.retrieveAllEntitiesLight p @"account"
    |> Seq.length > 0

(** We have have created our Delegate A/S publisher with "dg" as prefix *)
let tc5 () =
    try
      match (CrmData.Entities.retrievePublisher p @"dg").Id with
      | guid -> true
    with
      | _ -> false

(** We have have created our Delegate A/S solution "XrmOrg" *)
let tc6 () =
    try
      match (CrmData.Entities.retrieveSolution p @"XrmOrg").Id with
      | guid -> true
    with
      | _ -> false

(** We have have created our Delegate A/S solution "XrmOrg" and it has at least a couple of Web resources *)
let tc7 () =
    try
      match (CrmData.Entities.retrieveSolution p @"XrmOrg").Id with
      | guid -> 
        CrmData.Entities.retrieveWebResources p guid
        |> Seq.length > 0
    with
      | _ -> false

(** Root Business for this Organisation is XrmOrg *)
let tc8 () =
    (CrmData.Entities.retrieveRootBusinessUnit p)
        .Attributes.[@"name"] :?> string = AuthInfo.organization

(** Searching for XrmOrg should retrieve an entity with a name attribute that matches *)
let tc9 () =
    (CrmData.Entities.retrieveBusinessUnit p AuthInfo.organization)
        .Attributes.[@"name"] :?> string = AuthInfo.organization

(**
Run test cases
==============

Place test cases result in a list *)
let unitTest = [| tc1; tc2; tc3; tc4; tc5; tc6; tc7; tc8; tc9; |]
