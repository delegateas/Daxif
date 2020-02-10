(**
Sets up all the necessary variables and functions to be used for the other scripts. 
*)
#r @"bin\Microsoft.Xrm.Sdk.dll"
#r @"bin\Microsoft.Crm.Sdk.Proxy.dll"
#r @"bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll"
#r @"bin\Microsoft.Xrm.Tooling.Connector.dll"
#r @"bin\Delegate.Daxif.dll"
open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open DG.Daxif.Common.Utility
  
// Prompts the developer for a username and password the first time a script is run.
// It then stores these credentials in a local .daxif-file.
let creds = Credentials.FromKey("UserCreds")

// If you want to store login credentials directly in code, instead of in a local file, 
// replace the above line with the following
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
*)
module SolutionInfo =
  let name = @"XrmSolution"
  let displayName = @"XrmSolution"

module PublisherInfo =
  let prefix = @"dg"
  let name = @"delegateas"
  let displayName = @"Delegate A/S"


(** 
Path and project setup 
----------------------
*)
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

module Path =
  let daxifRoot = __SOURCE_DIRECTORY__
  let solutionRoot = daxifRoot ++ @"..\.."
  let toolsFolder = daxifRoot ++ @".."
  
  let webResourceProject = solutionRoot ++ @"WebResources"
  let webResourceFolder = 
    webResourceProject ++ @"src" ++ (sprintf "%s_%s" PublisherInfo.prefix SolutionInfo.name)
  
  let testProject = solutionRoot ++ @"Tests"
  let metdataFolder = testProject ++ @"Metadata"

  /// Path information used by the SolutionPackager scripts
  module SolutionPack =
    let projName = "SolutionBlueprint"
    let projFolder = solutionRoot ++ projName
    let xmlMappingFile = projFolder ++ (sprintf "%s.xml" SolutionInfo.name)
    let customizationsFolder = projFolder ++ @"customizations"
    let projFile = projFolder ++ (sprintf @"%s.csproj" projName)

  /// Paths Daxif uses to store/load files
  module Daxif =
    let crmSolutionsFolder = daxifRoot ++ "solutions"
    let unmanagedSolution = crmSolutionsFolder ++ (sprintf "%s.zip" SolutionInfo.name)
    let managedSolution = crmSolutionsFolder ++ (sprintf "%s_managed.zip" SolutionInfo.name)

    let translationsFolder = daxifRoot ++ "translations"
    let metadataFolder = daxifRoot ++ "metadata"
    let dataFolder = daxifRoot ++ "data"
    let stateFolder = daxifRoot ++ "state"
    let associationsFolder = daxifRoot ++ "associations"
    let mappingFolder = daxifRoot ++ "mapping"
    let importedFolder = daxifRoot ++ "imported"
  