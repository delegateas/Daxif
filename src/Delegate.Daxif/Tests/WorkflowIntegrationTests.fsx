(**
SolutionUnitTests.fsx
===========================

Load all libraries and .fs/.fsx files *)
//#load @"__temp__.fsx"
//#load @"AuthInfo.fsx"

(** Open libraries for use *)
open System
open System.IO
open System.Reflection
open Microsoft.Xrm.Sdk.Client
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

let pubPrefix = @"dg"
let pubName = @"delegateas"
let pubDisplay = @"Delegate A/S"
let solutionName = @"XrmOrg"
let solDisplay = @"XrmOrg"
let dll = AuthInfo.workflowPath

let dll'  = dll
let tmp   = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString() + @".dll")

File.Copy(dll',tmp,true)

let dllPath = Path.GetFullPath(tmp)
let dllName = Path.GetFileNameWithoutExtension(dll'); 

(** Instantiate service manager and service proxy *)
let m = ServiceManager.createOrgService wsdl
let tc = m.Authenticate(ac)
let p = ServiceProxy.getOrganizationServiceProxy m tc
let internal log = ConsoleLogger.ConsoleLogger LogLevel.Debug

try 
  CrmData.Entities.createPublisher p pubName pubDisplay pubPrefix |> ignore
  CrmData.Entities.createSolution p solutionName solDisplay pubPrefix |> ignore
with 
| ex -> ()
let asm = Assembly.LoadFile(dllPath); 
let activities = WorkflowHelper.getActivities asm solutionName |> Set.ofSeq

// Delete existing assembly
match CrmData.Entities.retrievePluginAssembly p AuthInfo.workflowDll with
| s when Seq.isEmpty s -> ()
| s ->
  s
  |> Seq.head
  |> fun x -> CrmData.CRUD.delete p x.LogicalName x.Id |> ignore

(**
Test cases
==============
*)


(* Create the workflow assembly in CRM and check that it exist in CRM *)
let tc0() =

  let solution = CrmData.Entities.retrieveSolution p solutionName

  PluginsHelper.instantiateAssembly solution dllName dllPath asm "" p log |> ignore

  // Test that the solution contains a workflow assembly
  CrmData.Entities.retrievePluginAssembly p AuthInfo.workflowDll 
  |> Seq.isEmpty 
  |> not

(* Create worfklow activities in CRM and check that they exist in CRM *)
let tc1() =

  // Fetch assembly id
  let asmId = 
    CrmData.Entities.retrievePluginAssembly p AuthInfo.workflowDll 
    |> Seq.head 
    |> fun x -> x.Id

  WorkflowHelper.createActivities asm asmId log m tc activities

  // Test that the solution contains a type
  activities
  |> Set.exists(fun x -> 
    match CrmData.Entities.tryRetrievePluginType p x with
    | Some(_) -> true
    | None -> false)

(* Delete workflow activities in CRM and check that they no longer exist in CRM *)
let tc2() =

  // Fetch assembly id
  let asmId = 
    CrmData.Entities.retrievePluginAssembly p AuthInfo.workflowDll 
    |> Seq.head 
    |> fun x -> x.Id
  
  let targetTypes = CrmData.Entities.retrievePluginTypes p asmId

  WorkflowHelper.deleteActivity log m tc (activities, targetTypes)

  //Test that it is Deleted
  activities
  |> Set.exists(fun x -> 
    match CrmData.Entities.tryRetrievePluginType p x with
    | Some(_) -> false
    | None -> true)

(* Delete the workflow assembly in CRM and check that it no longer exist in CRM *)
let tc3() =
    
  CrmData.Entities.retrievePluginAssembly p AuthInfo.workflowDll
  |> Seq.head
  |> fun x -> 
    CrmData.CRUD.delete p x.LogicalName x.Id |> ignore
    CrmData.Entities.existCrm p x.LogicalName x.Id None
    |> not


(**
Run test cases
==============

Place test cases result in a list *)
let unitTest = [| tc0; tc1; tc2; tc3;|]
