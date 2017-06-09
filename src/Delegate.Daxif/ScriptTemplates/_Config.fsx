(**
Config
======

Sets up all the necessary variables and functions to be used for the other scripts. 
*)
#r @"Microsoft.Xrm.Sdk.dll"
#r @"Delegate.Daxif.dll"
open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open DG.Daxif.Common.Utility

(** 
CRM Environment Setup 
---------------------
**)
   
let creds = Credentials.FromKey("UserCreds")

// If you want to store login credentials directly in code, instead of in a local file, replace the above line with the following
//let creds = Credentials.Create("usr", "pwd")

module Env =
  let dev = 
    Environment.Create(
      name = "Development",
      url = "https://mydev.crm4.dynamics.com/XRMServices/2011/Organization.svc",
      ap = AuthenticationProviderType.OnlineFederation,
      creds = creds,
      args = fsi.CommandLineArgs
    )
  
  let test = 
    Environment.Create(
      name = "Test",
      url = "https://mytest.crm4.dynamics.com/XRMServices/2011/Organization.svc",
      ap = AuthenticationProviderType.OnlineFederation,
      creds = creds,
      args = fsi.CommandLineArgs
    )

  let prod = 
    Environment.Create(
      name = "Production",
      url = "https://myprod.crm4.dynamics.com/XRMServices/2011/Organization.svc",
      ap = AuthenticationProviderType.OnlineFederation,
      creds = creds,
      args = fsi.CommandLineArgs
    )


(** 
CRM Solution Setup 
------------------
**)
module XrmSolution =
  let name = @"XrmSolution"
  let displayName = @"XrmSolution"

module Publisher =
  let prefix = @"dg"
  let name = @"delegateas"
  let displayName = @"Delegate A/S"


(** 
Path and project setup 
----------------------
**)
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

module Path =
  let daxifRoot = __SOURCE_DIRECTORY__
  let solutionRoot = daxifRoot ++ @"..\.."

  (* Code setup *)  
  // Where to find the plugin and workflow related assemblies and projects
  let pluginProjFile = solutionRoot ++ @"Plugins\Plugins.csproj"
  let pluginDll = solutionRoot ++ @"Plugins\bin\Release\ILMerged.Delegate.XrmOrg.XrmSolution.Plugins.dll"
  let workflowDll = solutionRoot ++ @"Workflow\bin\Release\ILMerged.Delegate.XrmOrg.XrmSolution.Workflow.dll"
  
  /// Where to place generated C# context
  let businessDomain = solutionRoot ++ @"BusinessDomain"

  (* Web resources *)
  let webResources = solutionRoot ++ @"WebResources"
  let webResourceSrc = webResources ++ @"src"
  
  /// Where to place generated declaration files from XrmDefinitelyTyped
  let xrmTypings = webResources ++ @"typings\XRM"

  /// Where to place XrmQuery javascript files
  let jsLib = webResourceSrc ++ (sprintf "%s_%s" Publisher.prefix XrmSolution.name) ++ "lib"
  

  (* Various tools *)
  let toolsFolder = daxifRoot ++ @".."
  let xrmContext = toolsFolder ++ @"XrmContext\XrmContext.exe"
  let xrmDefinitelyTyped = toolsFolder ++ @"XrmDefinitelyTyped\XrmDefinitelyTyped.exe"


  /// Paths used for SolutionPackager
  module SolPack =
    let projName = "XrmSolution"
    let projFolder = solutionRoot ++ projName
    let xmlMappingFile = projFolder ++ (sprintf "%s.xml" XrmSolution.name)
    let customizationsFolder = projFolder ++ @"customizations"
    let projFile = projFolder ++ (sprintf @"%s.csproj" projName)


  /// Paths Daxif uses to store/load files
  module Daxif =
    let crmSolutionsFolder = daxifRoot ++ "solutions"
    let unmanagedSolution = crmSolutionsFolder ++ (sprintf "%s.zip" XrmSolution.name)
    let managedSolution = crmSolutionsFolder ++ (sprintf "%s_managed.zip" XrmSolution.name)

    let translationsFolder = daxifRoot ++ "translations"
    let metadataFolder = daxifRoot ++ "metadata"
    let dataFolder = daxifRoot ++ "data"
    let stateFolder = daxifRoot ++ "state"
    let associationsFolder = daxifRoot ++ "associations"
    let mappingFolder = daxifRoot ++ "mapping"
    let importedFolder = daxifRoot ++ "imported"
  