namespace DG.Daxif.Common

open System
open System.IO
open System.Reflection
open DG.Daxif
open DG.Daxif.Common

module internal Resource = 
  let (|XML|Binary|Text|Unknown|) (s : string) = 
    let fileSplit = s.Split([| '.' |])
    let fileExtension = (fileSplit.[fileSplit.Length - 1]).ToUpper()
    try 
      let resourceType = Enum.Parse(typeof<WebResourceType>, fileExtension) :?> WebResourceType
      match resourceType.GetHashCode() with
      | 1 | 2 | 3 | 9 -> Text
      | 5 | 6 | 7 | 8 | 10 -> Binary
      | 4 -> XML
      | _ -> Unknown
    with _ -> Unknown
  
  let resources = 
    Assembly.GetExecutingAssembly().GetManifestResourceNames()
    |> Array.toList
    |> List.map (fun x -> 
         use stream = 
           Assembly.GetExecutingAssembly().GetManifestResourceStream(x)
         use sr = new StreamReader(stream)
         x, sr.ReadToEnd())
    |> Map.ofList
  
  let txtResources = 
    Assembly.GetExecutingAssembly().GetManifestResourceNames()
    |> Array.filter (function | Text -> true | _ -> false)
    |> Array.toList
    |> List.map (fun x -> 
         use stream = 
           Assembly.GetExecutingAssembly().GetManifestResourceStream(x)
         use sr = new StreamReader(stream)
         x, sr.ReadToEnd())
    |> Map.ofList
