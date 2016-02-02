#load @"..\src\Delegate.Daxif\Daxif.fs"
#load @"..\src\Delegate.Daxif\HelperModules\Common\ConsoleLogger.fs"
#load @"..\src\Delegate.Daxif\HelperModules\Common\Utility.fs"

open System
open System.Diagnostics
open System.IO
open System.Text
open DG.Daxif
open DG.Daxif.HelperModules.Common.Utility

// Get the directory to %PATH%
let p = Environment.GetEnvironmentVariable("PATH".ToLower());

let fs = @"C:\Program Files (x86)\Microsoft SDKs\F#\3.0\Framework\v4.0"
let fs' = "\"" + p + ";" + fs + "\""
let rd = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"

match p.Contains(fs) with
| false -> ()
| true -> 
    printfn "%O" ("Path already contains: \"" + fs + "\"")
    exit -1

let reg = @"reg.exe"
let args = "add " + rd + "/v Path /t REG_SZ /d " + fs'

let pr = executeProcess(reg, args)

match pr with
| (0,_,_) -> () //printfn "%O" pr.StdOut
| (_,os,es) -> failwith (es)

printfn "%O" (@"Path was succesfully updated with " + fs)
