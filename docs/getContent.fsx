// --------------------------------------------------------------------------------------
// Refreshes the content folder with updated files and flushes the output folder
// --------------------------------------------------------------------------------------

open System.IO

let (++) a b = Path.Combine(a, b)
let source = __SOURCE_DIRECTORY__
let content = source ++ @"content\"
let output = source ++ @"output\"

let toFetch = 
    [ source ++ @"..\src\Delegate.Daxif\Signatures", [@"*.fsi"]
      source ++ @"..\src\Delegate.Daxif", [@"*.fsx"]
      source ++ @"..\src\Delegate.Daxif\bin\Release", 
        [   @"Delegate.Daxif.dll"; @"Delegate.Daxif.xml"; 
            @"Microsoft.Xrm.Sdk.dll"; @"Microsoft.Xrm.Sdk.xml"]
      source ++ @"..\src", [ @"RELEASE_NOTES.md" ]
      source ++ @"..\src", [ @"LICENSE.md" ]
     ];;

// Clear previous content and output folders
printfn "Clearing content folder.."
Directory.EnumerateFiles(content, @"*.fs*")
|> Seq.iter (fun f -> File.Delete(f))

printfn "Copying over new content files.."
toFetch
|> Seq.collect (fun (d,patts) -> 
    Seq.collect (fun patt -> Directory.EnumerateFiles(d, patt)) patts)
|> Seq.iter(fun x -> File.Copy(x, content ++ Path.GetFileName(x), true))
