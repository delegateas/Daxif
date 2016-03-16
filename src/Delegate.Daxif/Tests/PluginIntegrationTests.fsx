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
open Microsoft.Xrm.Sdk
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

let pubPrefix =  AuthInfo.pubPrefix
let pubName =  AuthInfo.pubName
let pubDisplay =  AuthInfo.pubDisplay
let solutionName =  AuthInfo.solutionName
let solDisplay = AuthInfo.solDisplay
let dll = AuthInfo.pluginPath

let dll'  = dll
let tmp   = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString() + @".dll")

File.Copy(dll',tmp,true)

let dllPath = Path.GetFullPath(tmp)
let dllName = Path.GetFileNameWithoutExtension(dll'); 
(** Instantiate service manager and service proxy *)
let m = ServiceManager.createOrgService wsdl
let tc = m.Authenticate(ac)
let internal client = { PluginsHelper.IServiceM = m; PluginsHelper.authCred = tc}
let p = ServiceProxy.getOrganizationServiceProxy m tc
let internal log = ConsoleLogger.ConsoleLogger LogLevel.Debug
//let solution = CrmDataInternal.Entities.retrieveSolution p solutionName
let asm = Assembly.LoadFile(dllPath); 

let internal pluginEntity = PluginsHelper.typesAndMessages asm

match CrmDataInternal.Entities.retrievePluginAssembly p AuthInfo.pluginDll with
| s when Seq.isEmpty s -> ()
| s -> 
  s
  |> Seq.head
  |> fun x -> CrmData.CRUD.delete p x.LogicalName x.Id |> ignore

(**
Test cases
==============
*)

(* Create the plugin assembly in CRM and check that it exist in CRM *)
let tc0() =

  let solution = CrmDataInternal.Entities.retrieveSolution p solutionName

  PluginsHelper.instantiateAssembly solution dllName dllPath asm "" p log |> ignore

  // Test that the solution contains a plugins assembly
  CrmDataInternal.Entities.retrievePluginAssembly p AuthInfo.pluginDll 
  |> Seq.isEmpty 
  |> not

(* Create the plugin types in CRM and check if that they exist in CRM *)

let tc1() =
  
  // Fetch assembly id
  let asmId = 
    CrmDataInternal.Entities.retrievePluginAssembly p AuthInfo.pluginDll 
    |> Seq.head 
    |> fun x -> x.Id

  let pluginType =
    pluginEntity 
    |> Seq.map(fun p -> p.step.className) 
    |> Set.ofSeq

  PluginsHelper.createTypes log client pluginType asmId 

   // Test that the solution contains a type
  pluginType
  |> Set.exists(fun x -> 
    match CrmDataInternal.Entities.tryRetrievePluginType p x with
    | Some(_) -> true
    | None -> false)

(* Create the plugin steps in CRM and check if that they exist in CRM *)
let tc2() =

  let solution = CrmDataInternal.Entities.retrieveSolution p solutionName

  // Setup the needed data
  let pluginType = pluginEntity |> Seq.map(fun p -> p.step.className ) |> Seq.head |> CrmDataInternal.Entities.retrievePluginType p
  let pluginStep =
    pluginEntity |> Seq.map(fun p -> p.step.executionStage, PluginsHelper.messageName p.step) |> Set.ofSeq

  PluginsHelper.createPluginSteps log solution client pluginStep pluginEntity pluginType

   // Test that the solution contains a step
  CrmDataInternal.Entities.retrievePluginProcessingSteps p pluginType.Id
  |> Seq.isEmpty
  |> not

(* Create the plugin images in CRM and check that they exist in CRM *)
let tc3() =
  
  let solution = CrmDataInternal.Entities.retrieveSolution p solutionName

  // Setup the needed data
  let pluginStep = pluginEntity |> Seq.map(fun p -> PluginsHelper.messageName p.step) |> Seq.head |> CrmDataInternal.Entities.retrieveSdkProcessingStep p
  let pluginImage =
    pluginEntity 
    |> Seq.map(fun p -> p.images) 
    |> Seq.fold(fun acc i' -> 
          i' 
          |> Seq.map(fun image -> image.name) 
          |> Seq.append acc) Seq.empty
        |> Set.ofSeq
    |> Set.ofSeq

  PluginsHelper.createPluginImages log solution client pluginImage pluginEntity pluginStep

   // Test that the solution contains a images
  CrmDataInternal.Entities.retrieveAllPluginProcessingSteps p solution.Id
  |> Seq.exists(fun e -> 
    CrmDataInternal.Entities.retrievePluginProcessingStepImages p e.Id
    |> Seq.isEmpty
    |> not)

// TODO:
(* Update the plugin steps in an existing step in the CRM solution and check that it has been update*)
let tc4() =
    //PluginsHelper.updatePluginSteps
    true
// TODO:
(* Update the plugin steps in an existing step in the CRM solution and check that it has been update*)
let tc5() =
    //PluginsHelper.updatePluginImages
    true

(* Delete plugin images in CRM and check that they no longer exist in CRM *)
let tc6() =

  let solution = CrmDataInternal.Entities.retrieveSolution p solutionName

  // Setup the needed data
  let pluginStep = 
    pluginEntity 
    |> Seq.map(fun p -> PluginsHelper.messageName p.step) 
    |> Seq.head |> CrmDataInternal.Entities.retrieveSdkProcessingStep p
  let targetImages = CrmDataInternal.Entities.retrievePluginProcessingStepImages p pluginStep.Id
  let deleteImages =
    pluginEntity 
    |> Seq.map(fun p -> p.images) 
    |> Seq.fold(fun acc i' -> 
          i' 
          |> Seq.map(fun image -> image.name) 
          |> Seq.append acc) Seq.empty
       |> Set.ofSeq
    |> Set.ofSeq

  PluginsHelper.deleteImages log client (deleteImages |> Set.toSeq, targetImages)

  // Test that the solution no longer contains images
  CrmDataInternal.Entities.retrieveAllPluginProcessingSteps p solution.Id
  |> Seq.exists(fun e -> 
    CrmDataInternal.Entities.retrievePluginProcessingStepImages p e.Id
    |> Seq.isEmpty)

(* Delete plugin steps in CRM and check that they no longer exist in CRM *)

let tc7() =

  let solution = CrmDataInternal.Entities.retrieveSolution p solutionName

  let pluginType = pluginEntity |> Seq.map(fun p -> p.step.className ) |> Seq.head |> CrmDataInternal.Entities.retrievePluginType p
  let targetSteps = 
    CrmDataInternal.Entities.retrieveAllPluginProcessingSteps p solution.Id
    |> Seq.map(fun (x:Entity) -> 
      let stage = x.Attributes.["stage"] :?> OptionSetValue
      stage.Value, x)
    |> Array.ofSeq |> Seq.ofArray
  let deleteStep =
    pluginEntity |> Seq.map(fun p -> p.step.executionStage, PluginsHelper.messageName p.step) |> Set.ofSeq

  PluginsHelper.deleteSteps log client (deleteStep, targetSteps)

  // Test that the solution no longer contains steps
  CrmDataInternal.Entities.retrievePluginProcessingSteps p pluginType.Id
  |> Seq.isEmpty

(* Delete plugin types in CRM and check that they no longer exist in CRM *)
let tc8() =
  
  let asmId = 
    CrmDataInternal.Entities.retrievePluginAssembly p AuthInfo.pluginDll 
    |> Seq.head 
    |> fun x -> x.Id

  let targetTypes = CrmDataInternal.Entities.retrievePluginTypes p asmId
  let pluginType =
    pluginEntity |> Seq.map(fun p -> p.step.className ) |> Set.ofSeq

  PluginsHelper.deleteTypes log client (pluginType, targetTypes)

  // Test that the solution no longer contains types
  pluginType
  |> Set.exists(fun x -> 
    match CrmDataInternal.Entities.tryRetrievePluginType p x with
    | Some(_) -> false
    | None -> true)

(* Delete the plugin assembly in CRM and check that it no longer exist in CRM *)
let tc9() =
    
  CrmDataInternal.Entities.retrievePluginAssembly p AuthInfo.pluginDll
  |> Seq.head
  |> fun x -> 
    CrmData.CRUD.delete p x.LogicalName x.Id |> ignore
    CrmDataInternal.Entities.existCrm p x.LogicalName x.Id None
    |> not

(**
Run test cases
==============

Place test cases result in a list *)
let unitTest = [| tc0; tc1; tc2; tc3; tc4; tc5; tc6; tc7; tc8; tc9;|]