module Delegate.Daxif.Test.Export

open Xunit
open System
open System.IO
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common.Utility
open OrganizationMock


let mockExport (req: Messages.ExportSolutionRequest) = 
  let solutionPath = __SOURCE_DIRECTORY__ ++ "resources/export/" ++ (sprintf "%s.zip"req.SolutionName)
  match File.Exists(solutionPath) with
  | false -> 
    raise (FileNotFoundException (sprintf "Solution %s not found" req.SolutionName))
  | true -> 
    let solution = File.ReadAllBytes(solutionPath)
    let resp = Messages.ExportSolutionResponse()
    resp.Results.Add("ExportSolutionFile", solution)
    resp :> OrganizationResponse

type internal ExportOrg () = 
  [<DefaultValue>] val mutable exportedCalls : int
  inherit BaseMockOrg()
  interface IOrganizationService with
    override this.Execute( req: OrganizationRequest ) = 
      match req with
      | :? Messages.ExportSolutionRequest -> 
        this.exportedCalls <- this.exportedCalls + 1
        mockExport (req :?> Messages.ExportSolutionRequest)
      | x -> 
        printfn "Request %s not implemented" x.RequestName
        raise (System.NotImplementedException (sprintf "Request %s not implemented" x.RequestName))

[<Fact>]
let TestExport() =
  printfn "start"
  Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
  let org = ExportOrg()
  let mockEnv = EnvironmentMock.CreateEnvironment org
  let outFold =  __SOURCE_DIRECTORY__ ++ "./resources/temp/"
  Directory.CreateDirectory outFold |> ignore

  Assert.Equal(0, org.exportedCalls)

  let sol = Solution.Export(mockEnv, "SalesManagement", outFold, false, extended = false)

  Assert.Equal(1, org.exportedCalls)
  Assert.True (File.Exists sol)
  Directory.Delete(outFold, true)



