module DG.Daxif.Common.Utility

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open DG.Daxif

/// Option mapper
let (?|>) m f = Option.map f m
/// Option binder
let (?>>) m f = Option.bind f m
/// Option checker
let (?>>?) m c = Option.bind (fun x -> match c x with | true -> Some x | false -> None) m
/// Option default argument
let (?|) = defaultArg

/// If first option is none, choose second option
let (?|?) m1 m2 = match m1 with | Some _ -> m1 | None -> m2
/// If option is none, do second argument
let (?|>-) m f = match m with | Some _ -> () | None -> f()
/// If option is some, do second argument
let (?|>+) m f = Option.map f m ?| ()

/// Use argument and pass it along
let (|>>) x g = g x; x


// Path combine
let (++) x1 x2 = Path.GetFullPath(Path.Combine(x1, x2))

/// Converts a nullable-object to Maybe monad
let objToMaybe a =
  match isNull a with
  | true  -> None
  | false -> Some a

/// Make a map from a sequence and a given key function
let makeMap keyFunc = 
  Seq.map (fun x -> keyFunc x, x) >> Map.ofSeq

/// Minus for maps (based on their keys)
let (--) toKeep toRemove = 
  toKeep |> Map.filter (fun k _ -> Map.containsKey k toRemove |> not)

/// Intersects two maps based on their keys
let mapKeyIntersect toKeep toRemove = 
  toKeep |> Map.filter (fun k _ -> Map.containsKey k toRemove)

/// Holds the result of a map diff
type MapDiff<'a, 'b, 'c when 'a : comparison> = {
  adds: Map<'a, 'b>
  differences: Map<'a, ('b * 'c)>
  deletes: Map<'a, 'c>
}

/// Merges/partitions two maps based their keys
let mapDiff source target comparer =
  let intersect, newSources = source |> Map.partition (fun k _ -> Map.containsKey k target)
  let differences = 
    intersect 
    |> Map.map (fun k v -> v, Map.find k target) 
    |> Map.filter (fun _ v -> v ||> comparer |> not)
  let oldTargets = target -- source

  { adds = newSources
    differences = differences
    deletes = oldTargets
  }

/// Prints sizes of a mergePartition
let printMergePartition category source target comparer (log: ConsoleLogger) =
  let diff = mapDiff source target comparer

  log.Info "%s to create: %d" category (Seq.length diff.adds)
  diff.adds |> Map.iter (fun k _ -> log.Verbose "\t%s" k)

  log.Info "%s with differences: %d" category (Seq.length diff.differences)
  diff.differences |> Map.iter (fun k _ -> log.Verbose "\t%s" k)

  log.Info "%s to delete: %d" category (Seq.length diff.deletes)
  diff.deletes |> Map.iter (fun k _ -> log.Verbose "\t%s" k)


/// Union to string
let unionToString (x : 'a) = 
  match FSharpValue.GetUnionFields(x, typeof<'a>, true) with
  | case, _ -> case.Name
  
let stringToEnum<'T> str = Enum.Parse(typedefof<'T>, str) :?> 'T

/// ISO-8601
let timeStamp() = 
  DateTime.Now.ToString("o")

/// Filename safe
let timeStamp'() = 
  timeStamp().Replace(":", String.Empty)
  
/// Only date
let timeStamp''() = 
  let ts = timeStamp()
  ts.Substring(0, ts.IndexOf("T"))
  
/// Converts an exception to stringm including all inner exception messages
let getFullException (ex : exn) = 
  let rec getFullException' (ex : exn) (level : int) = 
    ex.Message 
    + match ex.InnerException with
      | null -> String.Empty
      | ie -> 
        sprintf @" [Inner exception(%d): %s]" level 
          (getFullException' ie (level + 1))
  getFullException' ex 0
  
 
// Converts a sequence of key-value pairs to an argument string
let toArg (k,v) = 
  sprintf "/%s:\"%s\"" k v

let toArgString argFunc (args: seq<string * string>) = 
  args |> Seq.map argFunc |> String.concat " "

let toArgStringDefault args = toArgString toArg args


let executeProcessWithDir (exe, args, dir) = 
  let psi = new ProcessStartInfo(exe, args)
  psi.CreateNoWindow <- true
  psi.UseShellExecute <- false
  psi.RedirectStandardOutput <- true
  psi.RedirectStandardError <- true
  psi.WorkingDirectory <- dir
  let p = Process.Start(psi)
  let o = new StringBuilder()
  let e = new StringBuilder()
  p.OutputDataReceived.Add(fun x -> o.AppendLine(x.Data) |> ignore)
  p.ErrorDataReceived.Add(fun x -> e.AppendLine(x.Data) |> ignore)
  p.BeginErrorReadLine()
  p.BeginOutputReadLine()
  p.WaitForExit()
  p.ExitCode, o.ToString(), e.ToString()
  
let executeProcess (exe, args) = 
  let fn = Path.GetFileName(exe)
  let dir = DirectoryInfo(exe).FullName.Replace(fn, "")
  (exe, args, dir) |> executeProcessWithDir


let printProcessHelper (ss: string) (log: ConsoleLogger) logl = 
  ss.Split('\n')
  |> Array.filter (fun x -> not (String.IsNullOrWhiteSpace x))
  |> Array.iter (fun x -> log.WriteLine(logl, x))
  
let printProcess proc (log : ConsoleLogger) = 
  function 
  | Some(0, os, _) -> printProcessHelper os log LogLevel.Info
  | Some(_, os, es) -> 
    printProcessHelper os log LogLevel.Info
    printProcessHelper es log LogLevel.Error
  | ex -> failwith (sprintf "%s threw an unexpected error: %A" proc ex)

// Prints the output and throws an exception if the process failed
let postProcess (code, es, os) log proc = 
  (code, es, os)
  |> Some
  |> printProcess proc log
  match code with
  | 0 -> ()
  | _ -> failwith (sprintf "%s failed" proc)

/// Active pattern to match regular expressions
let (|ParseRegex|_|) regex str = 
  let m = Regex(regex, RegexOptions.IgnoreCase).Match(str)
  match m.Success with
  | true -> 
    Some(List.tail [ for x in m.Groups -> x.Value ])
  | false -> None 


(* Argument handling *)
/// Parses an argument
let parseArg input = 
  Regex("^[/\-]?([^:=]+)((:|=)\"?(.*?)\"?)?$").Match(input)
  |> fun m -> m.Groups.[1].Value.ToLower(), m.Groups.[4].Value

/// Parses an array of argument
let parseArgs =
  Array.map parseArg
  >> Map.ofArray

/// Tries to find an argument that matches one of the specified strings in the toFind parameter
let tryFindArg toFind argMap =
  toFind
  |> Seq.tryPick (fun arg -> Map.tryFind arg argMap)

/// Alters a given filename of a path with the mapping function
let alterFilenameInPath path mapper =
  let newFileName = Path.GetFileNameWithoutExtension path |> mapper
  let dir = Path.GetDirectoryName path
  let ext = Path.GetExtension path
  dir ++ (newFileName + ext)

/// Adds specified ending to the end of the filename found in the path
let addEndingToFilename path ending =
  alterFilenameInPath path (fun s -> s + ending)

 
/// Parses a string to a Maybe int
let parseInt str =
  let mutable intvalue = 0
  if System.Int32.TryParse(str, &intvalue) then Some(intvalue)
  else None

/// Parses a string into a Version
let parseVersion (str:string): Version =
  let vArr = str.Split('.')
  let getIdx idx = Array.tryItem idx vArr ?>> parseInt ?| 0
  (getIdx 0, getIdx 1, getIdx 2, getIdx 3)

  
let getIntGroup def (m:Match) (idx:int) = parseInt m.Groups.[idx].Value ?| def
let getMinVersion = getIntGroup 0
let getMaxVersion = getIntGroup Int32.MaxValue
let criteriaRegex = Regex(@"^(?:(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:\.(\d+))?)?-(?:(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:\.(\d+))?)?$")

let parseVersionCriteria criteria: VersionCriteria option =
  let m = criteriaRegex.Match(criteria)
  match m.Success with
  | false -> None
  | true ->

    let fromVersion =
      match m.Groups.[1].Success with
      | false -> None
      | true  -> Some (getMinVersion m 1, getMinVersion m 2, getMinVersion m 3, getMinVersion m 4)

    let toVersion =
      match m.Groups.[5].Success with
      | false -> None
      | true  -> Some (getMaxVersion m 5, getMaxVersion m 6, getMaxVersion m 7, getMaxVersion m 8)

    Some (fromVersion, toVersion)

let versionCompare ((a1,b1,c1,d1): Version) ((a2,b2,c2,d2): Version) =
  ([a1;b1;c1;d1], [a2;b2;c2;d2])
  ||> List.map2 (-)
  |> List.tryFind ((<>) 0)

/// Version greater than or equal to
let (.>=) v1 v2 =
  versionCompare v1 v2
  ?|> fun x -> x > 0
  ?| true

/// Version less than or equal to
let (.<=) v1 v2 =
  versionCompare v1 v2
  ?|> fun x -> x < 0
  ?| true

/// Version less than
let (.<) v1 v2 = 
  v1 .>= v2 |> not

/// Version greater than
let (.>) v1 v2 = 
  v1 .<= v2 |> not

/// Check if the version matches the version criteria
let matchesVersionCriteria (versionToCheck: Version) (criteria: VersionCriteria) =
  match criteria with
  | None, None       -> true
  | Some v1, None    -> v1 .<= versionToCheck
  | None, Some v2    -> versionToCheck .< v2
  | Some v1, Some v2 -> v1 .<= versionToCheck && versionToCheck .< v2


/// Parses a string to a version increment, which is either "major", "minor", "build", or "revision"
let getVersionIncrement (str: string) =
  match str.Trim().ToLower() with
  | "major" -> Major
  | "minor" -> Minor
  | "build" -> Build
  | _       -> Revision
  