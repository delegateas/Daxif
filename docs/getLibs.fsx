#load @"..\src\Delegate.Daxif\Daxif.fs"

open System
open System.Diagnostics
open System.IO
open System.Text
open DG.Daxif

let rootFolder = __SOURCE_DIRECTORY__

Directory.EnumerateFiles(rootFolder + @"\lib\", @"*.dll", SearchOption.AllDirectories)
|> Seq.iter(fun x -> File.Copy(x, rootFolder + @"\..\lib\" + Path.GetFileName(x), true))

printfn "%O" @"Copied all assemblies to ..\lib\"
