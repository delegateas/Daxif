// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------
#r "packages/FAKE/tools/FakeLib.dll"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.NuGet.NuGet
open Fake.IO
open Fake.IO.Globbing.Operators
open System
open System.IO

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------
// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Delegate.Daxif"
// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Delegate Automated xRM Installation Framework"
// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let projectScripts = "Delegate.Daxif.Scripts"
// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summaryScripts = "Example Scripts for Daxif. Delegate Automated xRM Installation Framework"
// Company and copyright information
let company = "Delegate"
let copyright = @"Copyright (c) Delegate A/S 2017"
// File system information 
// (<solutionFile>.sln is built during the building process)
let solutionFile = @"Delegate.Daxif"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------
// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let (++) x y = Path.Combine(x,y)
let release = Fake.Core.ReleaseNotes.parse (IO.File.ReadAllLines (project + "/RELEASE_NOTES.md"))
let releaseScripts = Fake.Core.ReleaseNotes.parse (IO.File.ReadAllLines (projectScripts + "/RELEASE_NOTES.md"))


// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" 
  (fun _ -> 
  let daxifAssemblyInfoFileName = project + "/AssemblyInfo.fs"
  AssemblyInfoFile.createFSharp daxifAssemblyInfoFileName
    [ Fake.DotNet.AssemblyInfo.Title project
      Fake.DotNet.AssemblyInfo.Product project
      Fake.DotNet.AssemblyInfo.Description summary
      Fake.DotNet.AssemblyInfo.Company company
      Fake.DotNet.AssemblyInfo.Copyright copyright
      Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
      Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ]
  let scriptsAssemblyInfoFileName = projectScripts + "/AssemblyInfo.fs"
  AssemblyInfoFile.createFSharp scriptsAssemblyInfoFileName
    [ Fake.DotNet.AssemblyInfo.Title projectScripts
      Fake.DotNet.AssemblyInfo.Product projectScripts
      Fake.DotNet.AssemblyInfo.Description summaryScripts
      Fake.DotNet.AssemblyInfo.Company company
      Fake.DotNet.AssemblyInfo.Copyright copyright
      Fake.DotNet.AssemblyInfo.Version releaseScripts.AssemblyVersion
      Fake.DotNet.AssemblyInfo.FileVersion releaseScripts.AssemblyVersion ])

// --------------------------------------------------------------------------------------
// Setting up VS for building with FAKE
let commonBuild solution =
  let packArgs (defaults:MSBuild.CliArguments) = defaults
  solution
  |> DotNet.build (fun buildOp -> 
    { buildOp with 
          MSBuildParams = packArgs buildOp.MSBuildParams
    })

let commonNuget (rel: ReleaseNotes.ReleaseNotes) noBuild proj =
  let packArgs (defaults:MSBuild.CliArguments) = 
    { defaults with
        NoWarn = Some(["NU5100"])
        Properties = 
        [
          "Version", rel.NugetVersion
          "ReleaseNotes", String.Join(Environment.NewLine, rel.Notes)
        ] 
    }
  DotNet.pack (fun def -> 
    { def with
        NoBuild = noBuild
        MSBuildParams = packArgs def.MSBuildParams
        OutputPath = Some("bin")
    }) proj


let commonPublish nugetPackage = 
  let setNugetPushParams (defaults:NuGetPushParams) =
    { defaults with
        ApiKey = Fake.Core.Environment.environVarOrDefault "delegateas-nugetkey" "" |> Some
        Source = Some "https://api.nuget.org/v3/index.json"
    }
  let setParams (defaults:DotNet.NuGetPushOptions) =
      { defaults with
          PushParams = setNugetPushParams defaults.PushParams
       }
  DotNet.nugetPush setParams nugetPackage


let currentTimeFileFormat () = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")

Target.create "Clean" (fun _ -> 
  Shell.cleanDirs [ "bin"; "temp"; "Delegate.Daxif/bin"; "Delegate.Daxif.Scripts/bin"]
  
  let extList = ["dll"; "exe"; "pdb"; "xml"; "optdata"; "sigdata"]
  let matchesAnyExt (path: string) = extList |> List.exists (fun ext -> path.EndsWith(sprintf ".%s" ext))

  let tempDaxifPath = Path.Combine(Path.GetTempPath(), "Daxif")
  let scriptFolder = "Delegate.Daxif.Scripts/ScriptTemplates"
  Directory.CreateDirectory tempDaxifPath |> ignore
  Directory.CreateDirectory scriptFolder |> ignore

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

  tryDeleteFromFolder tempDaxifPath
  tryDeleteFromFolder scriptFolder
  moveFilesToTemp scriptFolder
)

Target.create "Build" (fun _ -> 
  !!(solutionFile + ".sln")
  |> Seq.head
  |> commonBuild
)

// --------------------------------------------------------------------------------------
// Build a NuGet package
Target.create "NuGetDaxif" (fun _ -> 
  commonNuget release true project)

Target.create "NuGetScripts" (fun _ -> 
  commonNuget releaseScripts false projectScripts)

// Publish the build nuget package
Target.create "PublishNuGetDaxif" (fun _ -> 
  let nugetPackage = !!("bin/"+project+"."+release.AssemblyVersion+".nupkg") |> Seq.head
  commonPublish nugetPackage)

Target.create "PublishNuGetScripts" (fun _ -> 
  let nugetPackage = !!("bin/"+projectScripts+"."+releaseScripts.AssemblyVersion+".nupkg") |> Seq.head
  commonPublish nugetPackage)

Target.create "Nuget" ignore
Target.create "PublishNuget" ignore

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "NuGetDaxif" <=> "NuGetScripts"
  ==> "Nuget"

"NuGetDaxif"
  ==> "PublishNuGetDaxif"

"NuGetScripts"
  ==> "PublishNuGetScripts"

"PublishNuGetDaxif" <=> "PublishNuGetScripts"
  ==> "PublishNuget"

Target.runOrDefault "Build"
