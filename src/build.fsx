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
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"
// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Delegate.Daxif"
// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Delegate Automated xRM Installation Framework"
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
let release = Fake.Core.ReleaseNotes.parse (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" 
  (fun _ -> 
  let fileName = project + "/AssemblyInfo.fs"
  AssemblyInfoFile.createFSharp fileName
    [ Fake.DotNet.AssemblyInfo.Title project
      Fake.DotNet.AssemblyInfo.Product project
      Fake.DotNet.AssemblyInfo.Description summary
      Fake.DotNet.AssemblyInfo.Company company
      Fake.DotNet.AssemblyInfo.Copyright copyright
      Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
      Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ])

// --------------------------------------------------------------------------------------
// Setting up VS for building with FAKE
let commonBuild solution =
  let packArgs (defaults:MSBuild.CliArguments) = 
    { defaults with
        NoWarn = Some(["NU5100"])
        Properties = 
        [
          "Version", release.NugetVersion
          "ReleaseNotes", String.Join(Environment.NewLine, release.Notes)
        ] 
    }
  solution
  |> DotNet.build (fun buildOp -> 
    { buildOp with 
          MSBuildParams = packArgs buildOp.MSBuildParams
    })


let currentTimeFileFormat () = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")

Target.create "Clean" (fun _ -> 
  Shell.cleanDirs [ "bin"; "temp"; "Delegate.Daxif/bin"]
  
  let extList = ["dll"; "exe"; "pdb"; "xml"; "optdata"; "sigdata"]
  let matchesAnyExt (path: string) = extList |> List.exists (fun ext -> path.EndsWith(sprintf ".%s" ext))

  let tempDaxifPath = Path.Combine(Path.GetTempPath(), "Daxif")
  let scriptFolder = "Delegate.Daxif/ScriptTemplates"
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
// Run the unit tests using test runner
Target.create "RunTests" ignore
// --------------------------------------------------------------------------------------
// Build a NuGet package
Target.create "NuGet" (fun _ -> 
  let packArgs (defaults:MSBuild.CliArguments) = 
    { defaults with
        NoWarn = Some(["NU5100"])
        Properties = 
        [
          "Version", release.NugetVersion
          "ReleaseNotes", String.Join(Environment.NewLine, release.Notes)
        ] 
    }
  DotNet.pack (fun def -> 
    { def with
        NoBuild = true
        MSBuildParams = packArgs def.MSBuildParams
        OutputPath = Some("bin")
        
    }) project)

// Publish the build nuget package
Target.create "PublishNuGet" (fun _ -> 
  let setNugetPushParams (defaults:NuGetPushParams) =
    { defaults with
        ApiKey = Fake.Core.Environment.environVarOrDefault "delegateas-nugetkey" "" |> Some
    }
  let setParams (defaults:DotNet.NuGetPushOptions) =
      { defaults with
          PushParams = setNugetPushParams defaults.PushParams
       }
  let nugetPacakge = !!("bin/*/"+project+"*.nupkg") |> Seq.head
  DotNet.nugetPush setParams nugetPacakge)

Target.create "GenerateReferenceDocs" ignore 
Target.create "GenerateHelp" ignore 
Target.create "GenerateHelpDebug" ignore
Target.create "KeepRunning" ignore
Target.create "AddLangDocs" ignore
Target.create "BuildPackage" ignore
Target.create "All" ignore

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "NuGet"
  ==> "BuildPackage"
  ==> "All"
  
"BuildPackage"
  ==> "PublishNuget"

Target.runOrDefault "Build"
