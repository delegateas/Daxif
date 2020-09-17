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
open Fake.Tools.Git
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
// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """A framework for automating a lot of xRM development processses.
  By using simple F# script commands/files one can save a lot of time
  and effort during this process by using Delegates DAXIF# library."""
// List of author names (for NuGet package)
let authors = [ "Delegate A/S" ]
// Tags for your project (for NuGet package)
let tags = "F# fsharp delegate crm xrm daxifsharp"
// Company and copyright information
let company = "Delegate"
let copyright = @"Copyright (c) Delegate A/S 2017"
// File system information 
// (<solutionFile>.sln is built during the building process)
let solutionFile = @"Delegate.Daxif"
// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"
// Git configuration (used for publishing documentation)
// The profile where the docs project is posted 
let docsGitHome = "https://github.com/delegateas/Daxif.wiki.git"

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
let commonBuild target solutions =
  let msBuildParams: MSBuild.CliArguments = 
    { (MSBuild.CliArguments.Create()) with
        Targets = [target]
        Properties = [("Configuration", "Release")]
    }
  solutions
  |> DotNet.build (fun x -> 
    {x with
       //Configuration = DotNet.BuildConfiguration.Custom(target)
       MSBuildParams = msBuildParams
       NoRestore = true
      })
  //try 
  //  solutions
  //  |> Seq.iter (fun solution ->
  //    let project = getBuildParamOrDefault "proj" solution
  //    let configuration = getBuildParamOrDefault "conf" "Release"
  //    let platform = getBuildParamOrDefault "plat" "AnyCPU"
  //    let setParams defaults =
  //      { defaults with
  //          Verbosity = Some(Quiet)
  //          Targets = [ target ]
  //          Properties =
  //            [
  //              "Configuration", configuration
  //              "Platform", platform
  //              "RealBuild", "true"
  //            ]
  //      }
  //    build setParams project
  //  )
  //with _ -> 
    //printfn "Unable to build with given params. Trying old MSBuildRelease."
    //solutions
    //|> Fake.DotNet.MSBuild.runRelease id "" target
    //|> ignore


let currentTimeFileFormat () = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")

Target.create "Clean" (fun _ -> 
  Shell.cleanDirs [ "bin"; "temp" ]
  
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
  
  !!(solutionFile + "*.sln")
  |> Seq.head
  |> commonBuild "Clean"
)

Target.create "Build" (fun _ -> 
  !!(solutionFile + ".sln")
  |> Seq.head
  |> commonBuild "Build"
)

Target.create "Rebuild" (fun _ -> 
  !!(solutionFile + "*.sln")
  |> Seq.head
  |> commonBuild "Rebuild"
)


// --------------------------------------------------------------------------------------
// Run the unit tests using test runner
Target.create "RunTests" ignore
// --------------------------------------------------------------------------------------
// Build a NuGet package
Target.create "NuGet" (fun _ -> 
  NuGet (fun p -> 
    { p with Title = project
             Authors = authors
             Project = project
             Summary = summary
             Description = description
             Copyright = copyright
             Tags = tags
             Version = release.NugetVersion
             ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
             OutputPath = "bin"
             NoDefaultExcludes = true
             AccessKey = Fake.Core.Environment.environVarOrDefault "delegateas-nugetkey" ""
             Dependencies = 
               [ ]
             References = [] }) (@"nuget/" + project + ".nuspec"))


// Build a NuGet package and publish it
Target.create "PublishNuGet" (fun _ -> 
  NuGet (fun p -> 
    { p with Title = project
             Authors = authors
             Project = project
             Summary = summary
             Description = description
             Copyright = copyright
             Tags = tags
             Version = release.NugetVersion
             ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
             OutputPath = "bin"
             PublishUrl = "https://www.nuget.org/api/v2/package"
             NoDefaultExcludes = true
             AccessKey = Fake.Core.Environment.environVarOrDefault "delegateas-nugetkey" ""
             Publish = Fake.Core.Environment.hasEnvironVar "delegateas-nugetkey"
             Dependencies = 
               [ ]
             References = [] }) (@"nuget/" + project + ".nuspec"))

Target.create "CleanDocs" (fun _ ->
  Shell.cleanDir "temp"
)

Target.create "GenerateDocs" (fun _ ->
    Directory.CreateDirectory("temp" ++ "code") |> ignore

    Directory.EnumerateFiles ("Delegate.Daxif" ++ "ScriptTemplates")
    |> Array.ofSeq
    |> Array.map (fun x -> (x.Split('\\') |> Array.last).Split('.') |> Array.head, File.ReadAllLines x)
    |> Array.map (fun (n,file) -> 
      n,[|"```"|]
      |> Array.append file
      |> Array.append [|"```fsharp"|] 
    )
    |> Array.iter (fun (n,file) ->
      File.WriteAllLines("temp" ++ "code" ++ n + ".md", file)   
    )
)

Target.create "ReleaseDocs" (fun _ ->
  Repository.cloneSingleBranch "" docsGitHome "master" ("temp" ++ "wiki")
  
  Directory.EnumerateFiles("temp" ++ "code")
  |> Array.ofSeq
  |> Array.map (fun d -> d.Split('\\') |> Array.last)
  |> Array.iter (fun f -> File.Copy("temp" ++ "code" ++ f, "temp" ++ "wiki" ++ f,true))

  Staging.stageAll ("temp" ++ "wiki")
  Commit.exec ("temp" ++ "wiki") "Updated wiki with new scripts"
  Branches.push ("temp" ++ "wiki")
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override
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

"CleanDocs"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"
  
Target.runOrDefault "Build"
