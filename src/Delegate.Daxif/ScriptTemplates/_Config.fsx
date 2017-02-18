(**
Config
======

Sets up all the necessary variables and functions to be used for the other
scripts. 
*)
#load @"_Setup.fsx"
open System
open System.IO
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open _Setup

(** 
CRM Environment Setup 
---------------------
**)
   
let globalCreds = Credentials.FromKey("Global")

let devEnv = 
  Environment.Create(
    name = "Development",
    url = "https://mydev.crm4.dynamics.com/XRMServices/2011/Organization.svc",
    ap = AuthenticationProviderType.OnlineFederation,
    creds = globalCreds,
    args = fsi.CommandLineArgs
  )
  
let testEnv = 
  Environment.Create(
    name = "Test",
    url = "https://mytest.crm4.dynamics.com/XRMServices/2011/Organization.svc",
    ap = AuthenticationProviderType.OnlineFederation,
    creds = globalCreds,
    args = fsi.CommandLineArgs
  )

let prodEnv = 
  Environment.Create(
    name = "Production",
    url = "https://myprod.crm4.dynamics.com/XRMServices/2011/Organization.svc",
    ap = AuthenticationProviderType.OnlineFederation,
    creds = globalCreds,
    args = fsi.CommandLineArgs
  )


(** 
CRM Solution Setup 
------------------
**)
let solutionName = @"XrmSolution"
let solutionDisplayName = @"XrmSolution"
let pubPrefix = @"dg"
let pubName = @"delegateas"
let pubDisplay = @"Delegate A/S"


(** 
Path and project setup 
----------------------
**)
module Path =
  let solutionRoot = daxifRoot ++ @"..\.."
  
  /// Where to find the plugin and workflow related assemblies and projects
  let pluginProjFile = solutionRoot ++ @"Plugins\Plugins.csproj"
  let pluginDll = solutionRoot ++ @"Plugins\bin\Release\ILMerged.Delegate.XrmOrg.XrmSolution.Plugins.dll"
  let workflowDll = solutionRoot ++ @"Workflow\bin\Release\ILMerged.Delegate.XrmOrg.XrmSolution.Workflow.dll"
  
  /// Web resource project
  let webResources = solutionRoot ++ @"WebResources"
  let webResourceSrc = webResources ++ @"src"
  
  /// Where to place generated declaration files from XrmDefinitelyTyped
  let xrmTypings = webResources ++ @"typings\XRM"

  /// Where to place XrmQuery javascript files
  let jsLib = webResourceSrc ++ (sprintf "%s_%s" pubPrefix solutionName) ++ "lib"
  
  /// Where to place generated C# context
  let businessDomain = solutionRoot ++ @"BusinessDomain"


  (* Setup paths for SolutionPackager *)
  let packProj = solutionRoot ++ "Blueprint"
  let packXmlMap   = packProj ++ "xml"
  let packCustomizations   = packProj ++ @"customizations"
  let packProjFile = packProj ++ @"Blueprint.csproj"

  
  (* Other tools *)
  let tools = solutionRoot ++ @"..\Tools"
  let xrmContext = tools ++ @"DG\XrmContext\XrmContext.exe"
  let xrmDefinitelyTyped = tools ++ @"DG\XrmDefinitelyTyped\XrmDefinitelyTyped.exe"


  (* Daxif related *)
  let crmSolutions = daxifRoot ++ "solutions"
  let translations = daxifRoot ++ "translations"
  let metadata = daxifRoot ++ "metadata"
  let data = daxifRoot ++ "data"
  let state = daxifRoot ++ "state"
  let associations = daxifRoot ++ "associations"
  let mapping = daxifRoot ++ "mapping"
  let imported = daxifRoot ++ "imported"
  