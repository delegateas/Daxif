// Based on: ovatsus / Setup.fsx - https://gist.github.com/ovatsus/2062683
// TODO: this currently loads fsproj's in alphabeticall order, we should instead
// build the dependencies graph of the fsproj's and load them in topological sort order
// Changed from StartWith to Contains in order to exclude FSharp.Core
module SetupTests

#r "System.Xml.Linq"

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq

let script = 
  seq { 
    for fsProjFile in Directory.GetFiles (__SOURCE_DIRECTORY__ + @"\..\", "*.fsproj") do
      let getElemName name = 
        XName.Get(name, "http://schemas.microsoft.com/developer/msbuild/2003")
      
      let getElemValue name (parent : XElement) = 
        let elem = parent.Element(getElemName name)
        if elem = null || String.IsNullOrEmpty elem.Value then None
        else Some(elem.Value)
      
      let getAttrValue name (elem : XElement) = 
        let attr = elem.Attribute(XName.Get name)
        if attr = null || String.IsNullOrEmpty attr.Value then None
        else Some(attr.Value)
      
      let (|??) (option1 : 'a Option) option2 = 
        if option1.IsSome then option1
        else option2
      
      let fsProjFile = 
        Directory.GetFiles(__SOURCE_DIRECTORY__ + @"\..\", "*.fsproj") 
        |> Seq.head
      let fsProjXml = XDocument.Load fsProjFile
      let regex = new Regex(Regex.Escape("..\\"))
      
      let refs = 
        fsProjXml.Document.Descendants(getElemName "Reference")
        |> Seq.choose 
             (fun elem -> 
             getElemValue "HintPath" elem |?? getAttrValue "Include" elem)
        |> Seq.filter (fun ref -> not (ref.Contains("FSharp.Core")))
        |> Seq.map 
             (fun ref -> 
             "#r \"" + (regex.Replace(ref, "..\\..\\", 1)).Replace(@"\", @"\\") 
             + "\"")
      
      let fsFiles = 
        fsProjXml.Document.Descendants(getElemName "Compile")
        |> Seq.choose (fun elem -> getAttrValue "Include" elem)
        |> Seq.filter (fun fsFile -> not (fsFile.EndsWith(".fsi")))
        |> Seq.map 
             (fun fsFile -> 
             "#load \"" + @"..\\" + fsFile.Replace(@"\", @"\\") + "\"")
      
      yield! refs
      yield! fsFiles
  }

let tempFile = Path.Combine(__SOURCE_DIRECTORY__, "__temp__.fsx")

match File.Exists(tempFile) with
| true -> File.Delete(tempFile)
| false -> ()
File.WriteAllLines(tempFile, script)

#load "__temp__.fsx"
