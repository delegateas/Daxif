(* For unit test to work a working environment need to be created first
 * The CRM demo needs to have example data installed
 * The related information needed are inputted in AuthInfo and along with resources needed for some unit tests
 * Resources:
 *  - PluginUnitTests
 *    Requires ILmerged plugin assembly dll with at least one type, step and image
 *    This assembly should be placed in the resource folder
 *
 *  - WorkflowUnitTests
 *    Requires ILmerged workflow assembly dll with at least one activity
 *    This assembly should be placed in the resource folder
 *
 *  - SolutionUnitTests
 *    Requires both an ILmerged plugin and workflow assembly dll 
 *    The plugin dll should contain at least one type, step and image
 *    The workflow dll should contain at least one activity
 *    These assembly should be placed in the resource folder
 *    Also requires a pakcaged solution placed in the resource folder
 *
 *  - WebResourceshelperUnitTests
 *    Requires a folder called "WebResources" placed in the resource folder
 *    This folder should contain atleast one javascript file
 *)

#load @"__temp__.fsx"
#load @"AuthInfo.fsx"

(**
Pre-build Unit Tests
====================

Load all UnitTest files *)

//#load "DataUnitTests.fsx"
//#load "MetadataIntegrationTests.fsx"
//#load "SolutionIntegrationTests.fsx"
#load "PluginIntegrationTests.fsx"
//#load "WorkflowIntegrationTests.fsx"
//#load "SerializationUnitTests.fsx"
//#load "WebResourcesHelperIntegrationTests.fsx"

(** Open libraries for use *)
open System


(**
Unittest setup
==============
*)

(**
Run test cases
==============

Place test cases result in a list *)
// TODO: test on a test environment
let unitTest = //[|"TODO", [| fun () -> true |] |]
  [| //"Data", DataUnitTests.unitTest;
//     "Metadata", MetadataIntegrationTests.unitTest;
//     "Serialization", SerializationUnitTests.unitTest;
//     "Solution", SolutionIntegrationTests.unitTest;
     "Plugin", PluginIntegrationTests.unitTest;
//     "Workflow", WorkflowIntegrationTests.unitTest;
//     "WebResources", WebResourcesHelperIntegrationTests.unitTest; 
  |]

let unitTest' = 
  unitTest
  |> Array.map (fun (x,ys) -> ys |> Array.map (fun y -> x,y()))
  |> Array.fold (fun a b -> Array.append a b) [||]

(** Combine results *)
let result = unitTest' |> Array.map(fun (_,xs) -> xs) |> Array.reduce (&&)

Convert.ToInt32(value = result) |> function 
| 1 -> printfn "Unit Tests: All test were executed successfully"
| _ -> failwith (sprintf "Some Unit Tests failed: %A" unitTest')
