(**
SolutionUnitTests.fsx
===========================

Load all libraries and .fs/.fsx files *)
//#load @"__temp__.fsx"
//#load @"AuthInfo.fsx"

(** Open libraries for use *)
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common

(**
Unittest setUp
==============

Setup of shared values *)

open System.IO

let usr = AuthInfo.usr
let pwd =  AuthInfo.pwd
let domain = AuthInfo.domain
let ap = AuthenticationProviderType.OnlineFederation
let wsdl = AuthInfo.wsdl

let pubPrefix = AuthInfo.pubPrefix
let pubName = AuthInfo.pubName
let pubDisplay = AuthInfo.pubDisplay
let solutionName = AuthInfo.solutionName
let solDisplay = AuthInfo.solDisplay

(** Instantiate service manager and service proxy *)
let ac = Authentication.getCredentials ap usr pwd domain
let m = ServiceManager.createOrgService wsdl
let tc = m.Authenticate(ac)
let p = ServiceProxy.getOrganizationServiceProxy m tc

let internal log = ConsoleLogger.ConsoleLogger LogLevel.Debug
let unmanaged = AuthInfo.resourceRoot + @"unmanaged\"
Utility.ensureDirectory unmanaged

(**
Test cases
==============
*)

(* We check if there exist a publisher or else we create a new *)
let tc1 () =
  
  
  try
    CrmData.Entities.retrievePublisher p pubPrefix |> ignore
    true
  with
  | _ -> 
    let ac' = Authentication.getCredentials ap usr pwd domain
    SolutionHelper.createPublisher' wsdl ac' pubName pubDisplay pubPrefix log
    let pubId = CrmData.Entities.retrievePublisher p pubPrefix |> fun x -> x.Id
    CrmData.Entities.existCrm p pubName pubId None  

(* We check if there exist a solution or else we create a new *)
let tc2 () =
  
  try
    CrmData.Entities.retrieveSolution p solutionName |> ignore
    true
  with
  | _ ->
    let ac' = Authentication.getCredentials ap usr pwd domain
    SolutionHelper.create' wsdl ac' solutionName solDisplay pubPrefix log
    let solutionId = CrmData.Entities.retrieveSolution p solutionName |> fun x -> x.Id
    CrmData.Entities.existCrm p solutionName solutionId None

(* We import a packaged solution into CRM and check the solution contains both plugin types, steps and images *)
let tc3 () =

  let zip = unmanaged + solutionName + @".zip"
  let ac' = Authentication.getCredentials ap usr pwd domain
  let solution = CrmData.Entities.retrieveSolution p solutionName

  try 
    SolutionHelper.import' wsdl ac' solutionName zip false log |> ignore

    //check that the solution contains types, steps and images
    let pluginsAsmid = ((CrmData.Entities.retrievePluginAssembly p AuthInfo.pluginDll) |> Seq.head).Id
    match CrmData.Entities.retrievePluginTypes p pluginsAsmid |> Seq.isEmpty with
    | true -> false
    | false -> 
      let steps = CrmData.Entities.retrieveAllPluginProcessingSteps p solution.Id
      match steps |> Seq.isEmpty with
      | true -> false
      | false ->
        steps
        |> Seq.exists(fun e -> 
          CrmData.Entities.retrievePluginProcessingStepImages p e.Id
          |> Seq.isEmpty
          |> not)
  finally
    // delete generated xml file
    Directory.GetFiles unmanaged
    |> Array.filter(fun x -> x.Contains(".xml"))
    |> Array.iter(fun x -> File.Delete x)

(* We export a solution from CRM and check if a packaged solution in the form of a zip file is created *)
let tc4 () =
  let ac' = Authentication.getCredentials ap usr pwd domain
  let exportPath = unmanaged + @"exported\"
  Utility.ensureDirectory exportPath
  let solution = CrmData.Entities.retrieveSolution p solutionName

  try

    SolutionHelper.export' wsdl ac' solutionName exportPath false log
    File.Exists(exportPath + solutionName + @".zip")

  finally
    
    let workflowAsmid = ((CrmData.Entities.retrievePluginAssembly p AuthInfo.workflowDll) |> Seq.head).Id
    let pluginsAsmid = ((CrmData.Entities.retrievePluginAssembly p AuthInfo.pluginDll) |> Seq.head).Id

    // Delete exported solution
    Directory.Delete(exportPath,true)
    // clear the imported plugins
    CrmData.Entities.retrievePluginTypes p pluginsAsmid
    |> Seq.iter(fun t -> 
      CrmData.Entities.retrieveAllPluginProcessingSteps p solution.Id
      |> Seq.iter(fun s -> 
        CrmData.Entities.retrievePluginProcessingStepImages p s.Id
        |> Seq.iter(fun i -> 
          CrmData.CRUD.delete p i.LogicalName i.Id |> ignore)
        CrmData.CRUD.delete p s.LogicalName s.Id |> ignore)
      CrmData.CRUD.delete p t.LogicalName t.Id |> ignore)

    // clear the imported workflow
    CrmData.Entities.retrievePluginTypes p workflowAsmid
    |> Seq.iter(fun a ->
      CrmData.CRUD.delete p a.LogicalName a.Id |> ignore )

    // Delete assemblies
    [AuthInfo.workflowDll; AuthInfo.pluginDll]
    |> List.map (CrmData.Entities.retrievePluginAssembly p)
    |> List.iter(fun s ->
      s
      |> Seq.head
      |> fun x -> CrmData.CRUD.delete p x.LogicalName x.Id |> ignore)

    

(**
Run test cases
==============

Place test cases result in a list *)
let unitTest = [| tc1; tc2; tc3; tc4; |]