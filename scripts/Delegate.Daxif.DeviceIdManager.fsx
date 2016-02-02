#load @"..\src\Delegate.Daxif\Daxif.fs"
#load @"..\src\Delegate.Daxif\HelperModules\Common\ConsoleLogger.fs"
#load @"..\src\Delegate.Daxif\HelperModules\Common\Utility.fs"

open System
open System.Diagnostics
open System.IO
open System.Text
open DG.Daxif
open DG.Daxif.HelperModules.Common.Utility

// Get the directory to %WINDIR%
let wd = Environment.GetEnvironmentVariable("WINDIR".ToLower());
let net = wd + @"\Microsoft.NET\Framework64\v4.0.30319"

let n = @"Microsoft.Crm.Services.Utility"
let n' = @"..\lib\" + n

match File.Exists(n' + @".dll") with
| false -> ()
| true -> File.Delete(n' + @".dll")

let csc = net + @"\csc.exe"
let args = "/optimize /target:library /out:\"" + n' + ".dll\" \"" + n' + ".cs\" "

let pr = executeProcess'(csc, args)

match fst pr with
| 0 -> () //printfn "%O" pr.StdOut
| _ -> failwith (snd pr)

printfn "%O" (n + @".dll was succesfully created")
