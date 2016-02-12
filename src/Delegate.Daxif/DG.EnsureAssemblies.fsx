(**
Ensure Assemblies
=================

Due to NuGets limitations when a VS project is restored, the "install.ps1" is not
executed and for this reason for this script, that will copy all the *.dll 
contained in the VS project "packages" folder to the folder containing the 
DAXIF# scripts

Open .NET libraries for use *)
open System.IO

/// Executed only once in order to ensure .dll from nuget packages are copied to .lib
let root =  __SOURCE_DIRECTORY__
let pkgs = root + @"/.." + @"/.." + @"/packages"

/// Exclude older or portable .NET versions
let blacklist = [ "net20"; "portable-" ]

let exclude (path:string) = 
  blacklist |> List.fold(fun a y -> path.Contains(y) || a) false |> not

let helper paths =
  paths
  |> Seq.filter(exclude)
  |> Seq.map(fun x -> Path.GetFullPath(x))
  |> Seq.map(fun x -> x,Path.Combine(root,Path.GetFileName(x)))
  |> Seq.iter(fun (x,y) -> File.Copy(x,y,true))

Directory.EnumerateFiles(pkgs, @"*.dll", SearchOption.AllDirectories) |> helper 
Directory.EnumerateFiles(pkgs, @"*.exe", SearchOption.AllDirectories) |> helper 

Directory.EnumerateFiles(pkgs, @"*.optdata", SearchOption.AllDirectories) |> helper
Directory.EnumerateFiles(pkgs, @"*.sigdata", SearchOption.AllDirectories) |> helper