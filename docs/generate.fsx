// --------------------------------------------------------------------------------------
// Builds the documentation from the files in the 'docs' directory
// --------------------------------------------------------------------------------------

#I "..\lib"
#r "FSharp.CodeFormat.dll"
#r "FSharp.Literate.dll"
#r "FSharp.MetadataFormat.dll"
open System
open System.IO
open System.Reflection
open FSharp.Literate
open FSharp.MetadataFormat

let website = "/Delegate.Daxif"

let (++) a b = Path.Combine(a, b)
let source = __SOURCE_DIRECTORY__
let output = source ++ @"output"

#if RELEASE
let root = website
#else
let root = "file://" + output
#endif

let content = source ++ @"content"
let resources = output ++ @"res"
let images = output ++ @"img"

let outputRef = output ++ @"reference"

let binaryPath = source ++ @"..\src\Delegate.Daxif\bin\Release"
let referenceBinaries = 
    [ binaryPath ++ @"Delegate.Daxif.dll" ]

let dependencies =
    [ binaryPath ++ @"Microsoft.Xrm.Sdk.dll" ]

// Flags for script/signature file generation
let options = 
    referenceBinaries @ dependencies
    |> List.fold (fun acc d -> (sprintf "--reference:\"%s\"" d)::acc) []
    |> String.concat " "

// Flags for API reference generation
let otherFlags = 
    dependencies
    |> List.fold (fun acc d -> (sprintf "-r:%s" d)::acc) []

let version =
    let a = Assembly.LoadFile(binaryPath ++ @"Delegate.Daxif.dll")
    a.GetName().Version.ToString()

//let formatting = __SOURCE_DIRECTORY__ ++ "../packages/FSharp.Formatting.2.4.1/"
let templates = source ++ "templates"
let docTemplate = templates ++ @"docpage.cshtml"

// Where to look for *.cshtml templates (in this order)
let layoutRoots =
  [ templates
    templates ++ "reference" ]

let examples = 
    Directory.EnumerateFiles(content, @"*.fsx")
    |> Seq.map Path.GetFileNameWithoutExtension
    |> String.concat ";"

// Additional strings to be replaced in the HTML template
let projInfo =
  [ "project-name", "DAXIF# v." + version;
    "page-author", "Delegate A/S";
    "page-description", "Delegate Automated Xrm Installation Framework (" + DateTime.Now.ToString("o") + ")"; 
    "project-author", "Delegate A/S"
    "project-github", "http://github.com/delegateas/delegateas.github.io"
    "project-page", "http://delegateas.github.io/"
    "project-nuget", "https://www.nuget.org/packages/Delegate.Daxif/"

    "project-summary", """
        A framework for automating installation of all kinds of xRM systems.
        By using simple F# script commands/files one can save a lot of time
        and effort during this process.
        """
    "examples", examples ]


let ensureDirectory dir = 
    match Directory.Exists dir with
    | false -> Directory.CreateDirectory dir |> ignore
    | true -> ()

let rec clearDirectory path = 
    Directory.EnumerateFiles(path)
    |> Seq.iter File.Delete
    Directory.EnumerateDirectories(path)
    |> Seq.iter(fun f -> clearDirectory f; Directory.Delete f);;


let clearOutputDirectory () =
    ensureDirectory output
    printfn "Clearing output folder.."
    clearDirectory output

let copyResources () =
    ensureDirectory resources
    Directory.EnumerateFiles(templates ++ "res")
    |> Seq.iter(fun f -> File.Copy(f, resources ++ Path.GetFileName(f), true))

let copyImages () =
    ensureDirectory images
    Directory.EnumerateFiles(templates ++ "img")
    |> Seq.iter(fun f -> File.Copy(f, images ++ Path.GetFileName(f), true))

// Build markdown documentation based on files in `docs/content`
let buildDoc () =
    Literate.ProcessDirectory
        ( inputDirectory = content,
          templateFile = docTemplate, 
          layoutRoots = layoutRoots,
          outputDirectory = output,
          format = OutputKind.Html,
          compilerOptions = options,
          replacements = ("root", root)::projInfo )

// Build API reference
let buildReference () =
  ensureDirectory outputRef
  Directory.EnumerateFiles(outputRef)
  |> Seq.iter (fun f -> File.Delete(f))
  MetadataFormat.Generate
    ( referenceBinaries,
      outputRef, layoutRoots, 
      parameters = ("root", root)::projInfo,
      otherFlags = otherFlags,
      markDownComments = false)

clearOutputDirectory()
copyResources()
copyImages()
buildDoc()
buildReference()

