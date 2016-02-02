(**
MetadataUnitTests.fsx
===========================

In order to execute this F# script F# 3.0 tools (VWD_FSharp.exe) must be 
installed. The tools can be downloaded from:

* [F# Tools for Visual Studio Express 2012 for Web][fst]

[fst]: http://go.microsoft.com/fwlink/?LinkId=261286

After installation run the following from a <code>cmd.exe</code> (run as Administrator):

    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" ^
        /v Path /t REG_SZ /d "%PATH%;C:\Program Files (x86)\Microsoft SDKs\F#\3.0\Framework\v4.0"

and then log off/login and the Register Database will be updated (no need for restart)
*)

(**
Libraries
=========

Load all libraries and .fs/.fsx files *)
//#load @"__temp__.fsx"
//#load @"AuthInfo.fsx"

(** Open libraries for use *)
open DG.Daxif.HelperModules

(**
Helper functions
----------------
*)

///// Append a slash between two strings
//let (+/) a b = a + @"/" + b
//
///// Fullpath to a file based on concatenating the absolute and relative paths
//let fullpath (a:string) r = a.Replace(@"\",@"/") +/ r
//
(**
Unittest setUp
==============

Setup of shared values *)

//let usr = AuthInfo.usr
//let pwd =  AuthInfo.pwd
//let domain = AuthInfo.domain
//let ap = AuthenticationProviderType.OnlineFederation
//let ac = Authentication.getCredentials ap usr pwd domain
//let wsdl = AuthInfo.wsdl//Uri(@"http://dg-crmdev-1.delegate.local:80/XrmOrg/XRMServices/2011/Organization.svc")

(** Instantiate service manager and service proxy *)
//let m = ServiceManager.createOrgService wsdl
//let tc = m.Authenticate(ac)
//let p = ServiceProxy.getOrganizationServiceProxy m tc

(** Point to the main XrmFrameworks web ressources *)
let location = AuthInfo.resourceRoot + @"\WebResources\src"
//let prefix = AuthInfo.pubPrefix
//let solutionName = AuthInfo.solutionName
//let publisher = CrmData.Entities.retrievePublisher p prefix
//let solution = CrmData.Entities.retrieveSolution p solutionName

(**
Test cases
==============

Retrieve all local ressources *)
let tc1 () =
    not (WebResourcesHelper.getLocalResourcesHelper location |> Set.ofSeq |> Set.isEmpty)
    
(**
Run test cases
==============

Place test cases result in a list *)
let unitTest = [| tc1; |]

//let unitTest' = unitTest |> Array.Parallel.map(fun x -> x())
//
//(** Combine results *)
//let result = unitTest' |> Array.reduce ( && )
//
//Convert.ToInt32(value = result) |> function
//    | 1 -> printfn "Unit Tests: All test were executed successfully"
//    | _ -> failwith (sprintf "Some Unit Tests failed: %A" unitTest')