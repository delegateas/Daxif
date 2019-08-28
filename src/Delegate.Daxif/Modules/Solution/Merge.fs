module internal DG.Daxif.Modules.Solution.Merge

open System
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open Microsoft.Crm.Sdk


// TODO: Make compatible with CRM 2016 service pack 1. 
// 2016 service pack 1 cause problem due to patching function changing the way
// solution components are defined
let mergeSolutions org ac sourceSolution targetSolution (log : ConsoleLogger) =
    
  let getName (x:Entity) = x.Attributes.["uniquename"] :?> string

  let isManaged (solution: Entity) = solution.Attributes.["ismanaged"] :?> bool

  let getSolutionComponents proxy (solution:Entity) =
    CrmDataInternal.Entities.retrieveAllSolutionComponenets proxy solution.Id
    |> Seq.map(fun x -> 
      let uid = x.Attributes.["objectid"] :?> Guid
      let ct = x.Attributes.["componenttype"] :?> OptionSetValue
      (uid, ct.Value))
    |> Set.ofSeq

  let m = ServiceManager.createOrgService org
  let tc = m.Authenticate(ac)
  use p = ServiceProxy.getOrganizationServiceProxy m tc

  log.WriteLine(LogLevel.Verbose, @"Service Manager instantiated")
  log.WriteLine(LogLevel.Verbose, @"Service Proxy instantiated")    

  // fail if 2016 service pack 1 is used
  let v, _ = CrmDataInternal.Info.version p 
  match v.[0], v.[2] with
  | '8','2' -> failwith "Not supported in CRM 2016 Service Pack 1" 
  | _, _ ->

    // Retrieve solutions
    let source =
      CrmDataInternal.Entities.retrieveSolutionAllAttributes p sourceSolution
    let target =
      CrmDataInternal.Entities.retrieveSolutionAllAttributes p targetSolution
    let sourceName = getName source
    let targetName = getName target

    // Ensure both solution are unmanaged
    match isManaged source, isManaged target with
    | true,_ ->
      failwith (sprintf "Unable to merge %s as it is a managed solution"
        sourceName)
    | _,true ->
      failwith (sprintf "Unable to merge %s as it is a managed solution"
        targetName)
    | _,_ ->

      // Retrieve entities in target and source
      let guidSource = getSolutionComponents p source
      let guidTarget = getSolutionComponents p target

      // Creating a mapping from objectid to the entity logicname
      let uidToLogicNameMap = 
        CrmData.Metadata.allEntities p
        |> Seq.map(fun x -> x.MetadataId, x.LogicalName)
        |> Seq.filter(fun (mid,_) -> mid.HasValue)
        |> Seq.map(fun (mid,ln) -> mid.Value, ln)
        |> Map.ofSeq

      // Find entities in target which does not exist in source
      let diff = guidTarget - guidSource

      // Make new solutioncomponent for source solution with entities in diff
      match diff.Count with
      | 0 -> 
        log.WriteLine(LogLevel.Info,
          sprintf @"Nothing to merge, components in %s already exist in %s"
            targetName sourceName)
      | x -> 
        log.WriteLine(LogLevel.Verbose,
          sprintf @"Adding %d component(s) from %s to the %s" x targetName sourceName)
        diff
        |> Seq.map(fun (uid,cType) ->
            
          log.WriteLine(LogLevel.Verbose,
            sprintf "Adding %s, %s to %s" 
              uidToLogicNameMap.[uid] (uid.ToString()) sourceName)

          let req = Messages.AddSolutionComponentRequest()
          req.ComponentId <- uid
          req.ComponentType <- cType
          req.SolutionUniqueName <- getName source
          req :> OrganizationRequest
          )
        |> Seq.toArray
        |> CrmDataInternal.CRUD.performAsBulkWithOutput p log