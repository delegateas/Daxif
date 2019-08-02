module internal DG.Daxif.Modules.WebResource.WebResourcesHelper

open System
open System.IO
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.InternalUtility

// Types of webresource actions
type WebResourceAction = 
  | Create
  | Update
  | Delete
  

let getMatchingEntitiesByName namesToKeep =
  Seq.filter (fun (x: Entity) -> namesToKeep |> Set.contains (x.GetAttributeValue<string>("name")))
  
// Convert a local web resource file to an entity object.
let localResourceToWebResource path name = 
  let ext = Path.GetExtension(path).ToUpper().Replace(@".", String.Empty)
  let webResourceType = Enum.Parse(typeof<WebResourceType>, ext.ToUpper()) :?> WebResourceType
  
  let wr = Entity("webresource")
  wr.Attributes.Add("content", fileToBase64 path)
  wr.Attributes.Add("displayname", Path.GetFileName name)
  wr.Attributes.Add("name", name)
  wr.Attributes.Add("webresourcetype", OptionSetValue(int webResourceType))

  match webResourceType with
  | WebResourceType.XAP -> wr.Attributes.Add("silverlightversion", "4.0")
  | _ -> ()

  match name.Contains(@"-") with // TODO: Do more complex HTML check
  | false -> Some wr
  | true -> 
    log.WriteLine(LogLevel.Error, "Webname: " + name + " is not supported")
    None
  
/// Get all local webresources by enumerating all folders at given location,
/// while looking for supported file types.
let getLocalResourcesHelper location crmRelease = 
  seq { 
    let exts = 
      Enum.GetNames(typeof<DG.Daxif.WebResourceType>)
      |> Array.map (fun x -> @"." + x.ToLower())
      |> Array.toList
      |> List.filter (fun x -> x <> ".svg" || crmRelease >= CrmReleases.D365)
      
    let rec getLocalResources' exts' = 
      seq { 
        match exts' with
        | [] -> ()
        | n0 :: tail -> 
          yield! Directory.EnumerateFiles(location, @"*" + n0, SearchOption.AllDirectories)
          yield! getLocalResources' tail
      }
      
    yield! getLocalResources' exts
  }
  
// Check for only one folder of the type: publishPrefix_uniqueSolutionName
let getPrefixAndUniqueName location = 
  Directory.GetDirectories(location)
  |> Array.toList
  |> function 
  | x :: [] -> 
    x.Substring(x.LastIndexOf(@"\") + 1).Split('_') 
    |> fun xs -> xs.[0], xs.[1]
  | _ -> 
    failwith 
      @"Incorrect root folder (must only contain 1 folder ex: 'publishPrefix_uniqueSolutionName'"
  
/// Filter out any files which are labeled with "_nosync"
let getLocalWRs location prefix crmRelease = 
  getLocalResourcesHelper location crmRelease
  |> Seq.filter (fun name -> not <| name.EndsWith("_nosync"))
  |> Seq.map (fun path -> 
    let name = path.Substring(path.IndexOf(location) + location.Length).Replace(@"\", "/").Trim('/')
    sprintf @"%s/%s" prefix name, path
  )
  |> Map.ofSeq
 
let getSyncActions proxy webresourceFolder solutionName =
  let (solutionId, prefix) = CrmDataInternal.Entities.retrieveSolutionIdAndPrefix proxy solutionName
  let webResources = CrmDataInternal.Entities.retrieveWebResources proxy solutionId
  
  let wrPrefix = sprintf "%s_%s" prefix solutionName

  let localWrPathMap = 
    CrmDataInternal.Info.version proxy
    |> snd
    |> getLocalWRs webresourceFolder wrPrefix
  let localWrs = localWrPathMap |> Seq.map (fun kv -> kv.Key) |> Set.ofSeq

  let crmWRs = 
    webResources
    |> Seq.map (fun x -> x.GetAttributeValue<string>("name"))
    |> Set.ofSeq

  let create = 
    localWrs - crmWRs
    |> Set.toArray
    |> Array.Parallel.map (fun name -> 
      let localWrPath = localWrPathMap.[name]
      localResourceToWebResource localWrPath name
    )
    |> Array.choose (fun x -> id x)
    |> Array.Parallel.map (fun x -> WebResourceAction.Create, x)
    
  let delete = 
    getMatchingEntitiesByName (crmWRs - localWrs) webResources
    |> Seq.toArray
    |> Array.Parallel.map (fun x -> WebResourceAction.Delete, x)
    
  let update = 
    getMatchingEntitiesByName (Set.intersect localWrs crmWRs) webResources
    |> Seq.toArray
    |> Array.Parallel.map (fun currentWr -> 
      let name = currentWr.GetAttributeValue<string>("name")
      let localWrPath = localWrPathMap.[name]
      let localWr = localResourceToWebResource localWrPath name
      currentWr, localWr
    )
    |> Array.choose (function 
      | _, None   -> None
      | u, Some v -> (u, v) |> Some
    )
    |> Array.Parallel.map (fun (currentWr, localWr) -> 
      let currentContent = currentWr.GetAttributeValue<string>("content")
      let localContent = localWr.GetAttributeValue<string>("content")
      currentWr.Attributes.["content"] <- localContent

      let currentDisplayName = currentWr.GetAttributeValue<string>("displayname")
      let localDisplayName = localWr.GetAttributeValue<string>("displayname")
      currentWr.Attributes.["displayname"] <- localDisplayName

      currentContent = localContent && currentDisplayName = localDisplayName, currentWr
    )
    |> Array.filter (fun (x, _) -> not x)
    |> Array.Parallel.map (fun (_, y) -> WebResourceAction.Update, y)
    
  seq { 
    yield! create
    yield! delete
    yield! update
  }

let syncSolution proxyGen location solutionName = 
  use p = proxyGen()
  
  let syncActions = getSyncActions p location solutionName
  
  let actionSuccess =
    syncActions
    |> Seq.toArray
    |> Array.Parallel.map (fun (x, y) -> 
          use p' = proxyGen()
          let yrn = y.GetAttributeValue<string>("name")
          try 
            match x with
            | WebResourceAction.Create -> 
              let pc = ParameterCollection()
              pc.Add("SolutionUniqueName", solutionName)
              let guid = CrmData.CRUD.create p' y pc
              log.Verbose "%s: (%O,%s) was created" y.LogicalName guid yrn

            | WebResourceAction.Update -> 
              CrmData.CRUD.update p' y |> ignore
              log.Verbose "%s: (%O,%s) was updated" yrn y.Id yrn

            | WebResourceAction.Delete -> 
              CrmData.CRUD.delete p' y.LogicalName y.Id |> ignore
              log.Verbose "%s: (%O,%s) was deleted" y.LogicalName y.Id yrn

            true
          with ex -> 
            match x with
            | WebResourceAction.Create -> 
              log.Error "%s: (%s) failed to create" y.LogicalName yrn
            | WebResourceAction.Update -> 
              log.Error "%s: (%O,%s) failed to update" yrn y.Id yrn
            | WebResourceAction.Delete -> 
              log.Error "%s: (%O,%s) failed to delete" y.LogicalName y.Id yrn

            log.WriteLine(LogLevel.Error, ex.Message.Replace(string y.Id, string y.Id + ", " + yrn))
            false
          )

  match (Seq.exists id actionSuccess), (Seq.exists not actionSuccess) with
  | false, false -> ()
  | false, true -> failwith "Nothing to publish, all changes failed"
  | true, fail -> 
    log.Verbose @"Publishing changes to the solution"
    CrmDataHelper.publishAll p

    match fail with
    | false -> 
      log.Verbose "All changes were successfully published"
    | true -> 
      log.Verbose "Some changes were successfully published"
      failwith "Some changes failed"
