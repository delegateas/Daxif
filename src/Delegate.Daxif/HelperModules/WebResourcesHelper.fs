namespace DG.Daxif.HelperModules

open System
open System.IO
open Microsoft.Xrm.Sdk
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.ConsoleLogger

module internal WebResourcesHelper = 
  // Types of webresource actions
  type WebResourceAction = 
    | Create
    | Update
    | Delete
  
  // Helpers functions
  let (+/) a b = a + @"/" + b
  let fullpath (a : string) r = a.Replace(@"\", @"/") +/ r
  
  let subset a (b : Entity seq) = 
    b |> Seq.filter (fun x -> 
           let z = x.Attributes.["name"] :?> string
           ((fun y -> y = z), a) ||> Seq.exists)
  
  // Convert a local web resource file to an entity object.
  let localResourceToWebResource file prefix solution 
      (log : ConsoleLogger.ConsoleLogger) = 
    let (log : ConsoleLogger) = log
    let ps = prefix + "_" + solution
    let fn = Path.GetFileName(file)
    let ext = Path.GetExtension(file).ToUpper().Replace(@".", String.Empty)
    let rp = file.Substring(file.IndexOf(ps) + ps.Length)
    let wn = ps + rp.Replace(@"\", @"/")
    let wt = 
      Enum.Parse(typeof<WebResourceType>, ext.ToUpper()) :?> WebResourceType
    let wr = Entity("webresource")
    wr.Attributes.Add("content", Utility.fileToBase64 (file))
    wr.Attributes.Add("displayname", Path.GetFileName wn)
    wr.Attributes.Add("name", wn)
    wr.Attributes.Add("webresourcetype", OptionSetValue(int wt))
    match wt with
    | WebResourceType.XAP -> wr.Attributes.Add("silverlightversion", "4.0")
    | _ -> ()
    match wn.Contains(@"-") with // TODO: Do more complex HTML check
    | false -> wr |> Some
    | true -> 
      let msg = "Webname: " + wn + " is not supported"
      log.WriteLine(LogLevel.Error, msg)
      None
  
  // Get all local webresources by enumerating all folders at given location,
  // while looking for supported file types.
  let getLocalResourcesHelper location = 
    seq { 
      let exts = 
        Enum.GetNames(typeof<DG.Daxif.WebResourceType>)
        |> Array.map (fun x -> @"." + x.ToLower())
        |> Array.toList
      
      let rec getLocalResources' exts' = 
        seq { 
          match exts' with
          | [] -> ()
          | n0 :: tail -> 
            yield! Directory.EnumerateFiles
                     (location, @"*" + n0, SearchOption.AllDirectories)
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
  
  /// Filter out any folders/files which are labeled with "_nosync" and
  /// reformat the filename.
  let localFiles location prefix solution = 
    getLocalResourcesHelper location
    |> Seq.map (fun x -> 
         let ps = prefix + "_" + solution
         let fn = Path.GetFileName(x)
         let rp = x.Substring(x.IndexOf(ps) + ps.Length)
         ps + rp.Replace(@"\", @"/"))
    |> Set.ofSeq
  
  let syncSolution' org ac location (log : ConsoleLogger.ConsoleLogger) = 
    let m = ServiceManager.createOrgService org
    let tc = m.Authenticate(ac)
    use p = ServiceProxy.getOrganizationServiceProxy m tc
    let (prefix, solutionName) = getPrefixAndUniqueName location
    let publisher = CrmData.Entities.retrievePublisher p prefix
    let solution = CrmData.Entities.retrieveSolution p solutionName
    let wr = CrmData.Entities.retrieveWebResources p solution.Id
    let source = localFiles location prefix solutionName
    
    let target = 
      wr
      |> Seq.map (fun x -> x.Attributes.["name"] :?> string)
      |> Set.ofSeq
    
    let create = source - target
    let delete = target - source
    let update = Set.intersect source target // only if different fnv1aHash
    
    let create' = 
      create
      |> Set.toArray
      |> Array.Parallel.map (fun x -> 
           localResourceToWebResource ((location, x) ||> fullpath) prefix 
             solutionName log)
      |> Array.choose (fun x -> id x)
      |> Array.Parallel.map (fun x -> WebResourceAction.Create, x)
    
    let delete' = 
      (delete, wr)
      ||> subset
      |> Seq.toArray
      |> Array.Parallel.map (fun x -> WebResourceAction.Delete, x)
    
    let update' = 
      (update, wr)
      ||> subset
      |> Seq.toArray
      |> Array.Parallel.map (fun x -> 
           let y = x.Attributes.["name"] :?> string
           let x' = 
             localResourceToWebResource ((location, y) ||> fullpath) prefix 
               solutionName log
           x, x')
      |> Array.choose (fun (x, y) -> 
           (x, y) |> function 
           | _, None -> None
           | u, Some v -> (u, v) |> Some)
      |> Array.Parallel.map (fun (x, y) -> 
           let x' = x.Attributes.["content"] :?> string
           let y' = y.Attributes.["content"] :?> string
           let h1 = x' |> Utility.fnv1aHash
           let h2 = y' |> Utility.fnv1aHash
           x.Attributes.["content"] <- y'
           let xdn = x.Attributes.["displayname"] :?> string
           let ydn = y.Attributes.["displayname"] :?> string
           x.Attributes.["displayname"] <- ydn
           h1 = h2 && xdn = ydn, x)
      |> Array.filter (fun (x, _) -> not x)
      |> Array.Parallel.map (fun (_, y) -> WebResourceAction.Update, y)
    
    seq { 
      yield! create'
      yield! delete'
      yield! update'
    }
    |> Seq.toArray
    |> Array.Parallel.iter (fun (x, y) -> 
         use p' = ServiceProxy.getOrganizationServiceProxy m tc
         let yrn = y.Attributes.["name"] :?> string
         try 
           match x with
           | WebResourceAction.Create -> 
             let pc = ParameterCollection()
             pc.Add("SolutionUniqueName", solutionName)
             let guid = CrmData.CRUD.create p' y pc
             let msg = sprintf "%s: (%O,%s) was created" y.LogicalName guid yrn
             log.WriteLine(LogLevel.Verbose, msg)
           | WebResourceAction.Update -> 
             CrmData.CRUD.update p' y |> ignore
             let msg = sprintf "%s: (%O,%s) was updated" yrn y.Id yrn
             log.WriteLine(LogLevel.Verbose, msg)
           | WebResourceAction.Delete -> 
             CrmData.CRUD.delete p' y.LogicalName y.Id |> ignore
             let msg = sprintf "%s: (%O,%s) was deleted" y.LogicalName y.Id yrn
             log.WriteLine(LogLevel.Verbose, msg)
         with ex -> log.WriteLine(LogLevel.Error, ex.Message))
    match create'.Length, delete'.Length, update'.Length with
    | (0, 0, 0) -> ()
    | _ -> 
      log.WriteLine(LogLevel.Verbose, @"Publishing changes to the solution")

      CrmData.CRUD.publish p

      log.WriteLine
        (LogLevel.Verbose, @"The changes were successfully published")
