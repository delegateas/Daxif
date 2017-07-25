module DG.Daxif.Modules.Solution.Versioning

open System
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

open DG.Daxif.Common.CrmDataHelper
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages


let incrementVersionNumber ((major, minor, build, rev): Version) increment : Version =
  match increment with
  | Revision -> major, minor, build, rev+1
  | Build    -> major, minor, build+1, 0
  | Minor    -> major, minor+1, 0, 0
  | Major    -> major+1, 0, 0, 0


let getSolutionVersionNumber proxy solutionName =
  CrmDataHelper.retrieveSolution proxy solutionName (RetrieveSelect.Fields [ "version" ])
  |> fun e ->
    e.Id, e.GetAttributeValue<string>("version") |> parseVersion


let updateSolutionVersionTo proxy (solutionId: Guid) (major, minor, build, rev) =
  let e = Entity("solution", solutionId)
  e.["version"] <- sprintf "%d.%d.%d.%d" major minor build rev
  
  CrmDataHelper.makeUpdateReq e
  |> CrmDataHelper.getResponse<UpdateResponse> proxy
  |> ignore

