(**
Ensure Assemblies
=================

Copy assemblies from project references to this folder

Open .NET libraries for use *)
#r "System.Xml.Linq"
open System
open System.Linq
open System.Xml.Linq
open System.IO


let extList = ["dll"; "exe"; "pdb"; "xml"; "optdata"; "sigdata"]


(** Clear out assembly files from this folder **)
let currentTimeFileFormat () = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")
let tempDaxifPath = Path.Combine(Path.GetTempPath(), "Daxif")
Directory.CreateDirectory tempDaxifPath |> ignore

let matchesAnyExt (path: string) = extList |> List.exists (fun ext -> path.EndsWith(sprintf ".%s" ext))

/// Remove old locked files if possible
let tryDeleteFromFolder dir =
  Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
  |> Seq.filter matchesAnyExt
  |> Seq.iter (fun path -> try File.Delete(path) with _ -> ())

/// Move potentially locked dlls
let moveFilesToTemp dir = 
  Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
  |> Seq.filter matchesAnyExt
  |> Seq.iter (fun path ->
    let filename = Path.Combine(tempDaxifPath, Path.GetFileNameWithoutExtension path)
    let ext = Path.GetExtension path
    let time = currentTimeFileFormat()
    let newPath = sprintf "%s.%s.old%s" filename time ext
    File.Move(path, newPath)
  )



(** Find necessary assemblies and copy them to this folder **)
let toMaybe x = if isNull x then None else Some x
let fromMaybe<'T when 'T : null> (x:Option<'T>) = if x.IsSome then x.Value else null
let tryFunc f = toMaybe >> Option.map f >> fromMaybe

let toSomeIf c x = if c x then Some x else None

let (?|>) m f = Option.map f m
let (?|) = defaultArg
let xn s = XName.Get(s)

/// Find project files in parent folders
let rec findProjectFiles dir = seq {
  yield! Directory.EnumerateFiles(dir, "*.??proj", SearchOption.TopDirectoryOnly)
  let newPath = Path.GetFullPath(Path.Combine(dir, ".."))
  if newPath <> dir then yield! findProjectFiles newPath
}

/// Read all references from a project file
let getProjectReferences (pathToProjFile: string) =
  let msbuild = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003")
  let projDefinition = XDocument.Load(pathToProjFile)
  projDefinition
    .Element(msbuild + "Project")
    .Elements(msbuild + "ItemGroup")
    .Elements(msbuild + "Reference")
    .Elements(msbuild + "HintPath") 
    |> Seq.map (tryFunc (fun x -> x.Value))
    |> Array.ofSeq;

/// Copy files to a target directory
let copyFilesWithExts targetDir exts =
  Seq.iter (fun orgPath -> 
    (Path.GetExtension orgPath |> fun x -> x.Remove(0,1)) :: exts 
    |> List.distinct
    |> List.map (fun ext -> Path.ChangeExtension(orgPath, ext))
    |> List.filter File.Exists
    |> List.iter (fun path ->
      let fn = Path.GetFileName(path)
      let target = Path.Combine(targetDir, fn)
      try File.Copy(path, target, true) with _ -> printfn "Could not copy '%s' to '%s'" path target
    )
  )

/// Check if a list of references includes Daxif
let hasDaxifReference refs =
  refs |> Seq.exists (fun (s: string) -> s.Contains("Delegate.Daxif"))

/// Fix paths to match that of the source
let fixPaths sourceDir =
  Seq.map (fun path -> Path.Combine(sourceDir, path))


/// Perform copy of referenced assemblies in the closest project file to the given folder
let copyProjectReferences targetDir =
  findProjectFiles targetDir 
  |> Seq.tryPick (fun projFile ->
    let sourceDir = Path.GetDirectoryName projFile
    getProjectReferences projFile |> toSomeIf hasDaxifReference ?|> fixPaths sourceDir
  )
  ?|> copyFilesWithExts targetDir extList
  |> function
  | None -> failwithf "Unable to find a project file with Daxif referenced."
  | _ -> ()
  

(** Perform the actions **)
let root = __SOURCE_DIRECTORY__
tryDeleteFromFolder tempDaxifPath
tryDeleteFromFolder root
moveFilesToTemp root

copyProjectReferences root