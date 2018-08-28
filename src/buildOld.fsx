﻿// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------
#r "packages/FAKE/tools/FakeLib.dll"
#r "FSharp.Literate.dll"

open Fake
open Fake.Git
open Fake.ReleaseNotesHelper
open Fake.AssemblyInfoFile
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
let docsGitHome = "https://github.com/delegateas/Daxif"
// The name of the project on GitHub
let docsGitName = "delegateas.github.io"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------
// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" 
  (fun _ -> 
  let fileName = project + "/AssemblyInfo.fs"
  CreateFSharpAssemblyInfo fileName 
    [ Attribute.Title project
      Attribute.Product project
      Attribute.Description summary
      Attribute.Company company
      Attribute.Copyright copyright
      Attribute.Version release.AssemblyVersion
      Attribute.FileVersion release.AssemblyVersion ])


// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages


Target "RestorePackages" RestorePackages
Target "CleanDocs" (fun _ -> CleanDirs [ "../docs/output" ])

// Setting up VS for building with FAKE
let commonBuild target solutions =
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
    solutions
    |> MSBuildRelease "" target
    |> ignore

let currentTimeFileFormat () = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")

Target "Clean" (fun _ -> 
  CleanDirs [ "bin"; "temp" ]
  
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
  |> commonBuild "Clean"
)

Target "Build" (fun _ -> 
  !!(solutionFile + ".sln")
  |> commonBuild "Build"
)

Target "Rebuild" (fun _ -> 
  !!(solutionFile + "*.sln")
  |> commonBuild "Rebuild"
)


// --------------------------------------------------------------------------------------
// Run the unit tests using test runner
Target "RunTests" (fun _ -> 
  !!testAssemblies |> NUnit(fun p -> 
                        { p with DisableShadowCopy = true
                                 TimeOut = TimeSpan.FromMinutes 20.
                                 OutputFile = "TestResults.xml" }))
// --------------------------------------------------------------------------------------
// Build a NuGet package
Target "NuGet" (fun _ -> 
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
             AccessKey = getBuildParamOrDefault "delegateas-nugetkey" ""
             Dependencies = 
               [ ]
             References = [] }) (@"nuget/" + project + ".nuspec"))


// Build a NuGet package and publish it
Target "PublishNuGet" (fun _ -> 
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
             AccessKey = getBuildParamOrDefault "delegateas-nugetkey" ""
             Publish = hasBuildParam "delegateas-nugetkey"
             Dependencies = 
               [ ]
             References = [] }) (@"nuget/" + project + ".nuspec"))

// --------------------------------------------------------------------------------------
// Generate the documentation
Target "GenerateDocs" 
  (fun _ -> 
  executeFSIWithArgs "../docs/" "getLibs.fsx" [] [] |> ignore
  executeFSIWithArgs "../docs/" "getContent.fsx" [ "--define:RELEASE" ] [] 
  |> ignore
  executeFSIWithArgs "../docs/" "generate.fsx" [ "--define:RELEASE" ] [] 
  |> ignore)
Target "GenerateDocsLocal" (fun _ -> 
  executeFSIWithArgs "../docs/" "getLibs.fsx" [] [] |> ignore
  executeFSIWithArgs "../docs/" "getContent.fsx" [] [] |> ignore
  executeFSIWithArgs "../docs/" "generate.fsx" [] [] |> ignore)


// --------------------------------------------------------------------------------------
// Release Scripts
Target "ReleaseDocs" (fun _ -> 
  let tempDocsDir = "temp/docs"
  let tempProjDocsDir = tempDocsDir @@ project
  CleanDir tempDocsDir
  Repository.cloneSingleBranch "" (docsGitHome + "/" + docsGitName + ".git") 
    "master" tempDocsDir
  fullclean tempProjDocsDir
  CopyRecursive "../docs/output" tempProjDocsDir true |> tracefn "%A"
  StageAll tempProjDocsDir
  Commit tempProjDocsDir 
    (sprintf "Update generated documentation for version %s" 
       release.NugetVersion)
  Branches.push tempDocsDir
)

Target "Release" DoNothing


// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override
Target "All" DoNothing
"Clean" ==> "RestorePackages" ==> "AssemblyInfo" ==> "Rebuild" ==> "All"
"All" ==> "CleanDocs" ==> "GenerateDocsLocal"
"All" ==> "CleanDocs" ==> "GenerateDocs" ==> "ReleaseDocs" ==> "Release"
"All" ==> "NuGet" ==> "PublishNuGet" ==> "Release"
RunTargetOrDefault "All"
