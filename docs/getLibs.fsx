#load @"..\src\Delegate.Daxif\Daxif.fs"
#load @"..\src\Delegate.Daxif\HelperModules\Common\ConsoleLogger.fs"
#load @"..\src\Delegate.Daxif\HelperModules\Common\Utility.fs"

open System
open System.Diagnostics
open System.IO
open System.Text
open DG.Daxif
open DG.Daxif.HelperModules.Common.Utility

let rootFolder = __SOURCE_DIRECTORY__

Directory.EnumerateFiles(rootFolder + @"\lib\", @"*.dll", SearchOption.AllDirectories)
|> Seq.iter(fun x -> File.Copy(x, rootFolder + @"\..\lib\" + Path.GetFileName(x), true))

printfn "%O" @"Copied all assemblies to ..\lib\"
