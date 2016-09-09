namespace DG.Daxif.HelperModules

open System
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq
open System.Net
open System.Web
open System.Text.RegularExpressions
open Microsoft.XmlDiffPatch
open Microsoft.Crm.Tools.SolutionPackager
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility
open DG.Daxif.HelperModules.Common.Resource
open DG.Daxif.HelperModules.Common.ConsoleLogger
open Suave
open Suave.Control
open Suave.Operators
open Suave.Http
open Suave.Successful
open Suave.Filters

module internal DiffHelper = 

  // MSDN: The XML Diff and Patch GUI Tool
  // https://msdn.microsoft.com/en-us/library/aa302295.aspx

  // Goal is to port CustomizationComparisonUtility.exe to MS CRM 20XX
  // MSDN: ISV Utilities for Comparing Customizations and Transferring
  //       Configuration Data (Microsoft Dynamics CRM 4.0)
  // https://msdn.microsoft.com/en-us/library/dd442453.aspx

  // Brainstorm:
  // 1) Input parameters are Source and Target solutions
  // 2) Create a unique folder int %TMP% with a source and target subfolders
  // 3) Extract both solutions to source and target folders
  //    REMARK: https://www.nuget.org/packages/Microsoft.CrmSdk.CoreTools/
  // 4) Check for differences between files
  //        let create = source - target
  //        let delete = target - source
  //        let update = Set.intersect source target
  //        REMARK: Update only if different fnv1aHash/SHA first pass and then XML Diff second pass
  // 5) Show changes as HTML with 

  let diffOptions () =
    XmlDiffOptions.None |||
    XmlDiffOptions.IgnorePI |||
    XmlDiffOptions.IgnoreComments |||
    XmlDiffOptions.IgnoreXmlDecl |||
    XmlDiffOptions.IgnoreWhitespace |||
    XmlDiffOptions.IgnoreDtd

  type DiffType = 
    | Unchanged = 0
    | Changed = 1
    | NotInSource = 2
    | NotInTarget = 3

  // encodes url and ensure that latters a upper case
  let urlEncodeUpper (url:string) =
    HttpUtility.UrlEncode url
    |> fun x ->  Regex.Replace( x, @"%[0-9a-f][0-9a-f]", (fun x -> x.Value.ToUpper()))

  // extract a solution from a package
  let extractSP zip path (log:ConsoleLogger) =
    let logl = Enum.GetName(typeof<LogLevel>,log.LogLevel)
    
    let pa = new PackagerArguments()

    log.WriteLine(LogLevel.Info,"Start output from SolutionPackager")

    // Use parser to ensure correct initialization of arguments
    Parser.ParseArgumentsWithUsage(
        [|"/action:Extract"; 
        sprintf @"/zipfile:%s" zip ;
        sprintf @"/folder:%s" path; 
        sprintf @"/errorlevel:%s" logl;
        "/allowDelete:Yes";
        "/clobber"|],
        pa) |> ignore
        
    try 
      let sp = new SolutionPackager(pa)
      sp.Run()  
    with 
      ex -> log.WriteLine(LogLevel.Error,sprintf "%s" ex.Message )
    
  // unpack the solution to a defined folder
  let unpackSolution zip (log:ConsoleLogger.ConsoleLogger) = 

    let folderName = Guid.NewGuid().ToString()

    let tmpFolder = Path.Combine(Path.GetTempPath(), folderName)

    tmpFolder |> Utility.ensureDirectory

    (zip,tmpFolder, log) |||> extractSP
    
    tmpFolder
 
  //Removes <!CDATA[]]> around text and replace whitespace with indentation
  let cleanHtmlTextDiff (str:string) = 
    str
      .Replace("&lt;!CDATA[","")
      .Replace("]]&gt;","")
      .Replace("    ","&emsp;")

  let changeColourScheme bs =
    new StringBuilder(Encoding.UTF8.GetString(bytes = bs))
    |> fun sb ->
      sb
        .Replace(
            "<font style=\"background-color: yellow\" color=\"black\">",
            "<font style=\"background-color: lime\" color=\"black\">")
        .Replace(
            "<font style=\"background-color: lightgreen\" color=\"black\">",
            "<font style=\"background-color: yellow\" color=\"black\">")
        .Replace(
            "<font style=\"background-color: red\" color=\"black\">",
            "<font style=\"background-color: red\" color=\"white\">")
        .Replace(
            "<font style=\"background-color: red\" color=\"blue\">",
            "<font style=\"background-color: silver\" color=\"black\">")
        .Replace(
            "<font style=\"background-color: yellow\" color=\"blue\">",
            "<font style=\"background-color: silver\" color=\"black\">")
        .Replace(
            "<font style=\"background-color: white\" color=\"#AAAAAA\">",
            "<font style=\"background-color: blue\" color=\"white\">")
    |> fun sb -> Encoding.UTF8.GetBytes(sb.ToString())

  // produce html text containing the sha hash of two packaged files
  let htmlBinaryShaDiff pathSource pathTarget = 
    let hashSource = 
      match File.Exists(pathSource) with 
      | true -> File.ReadAllBytes(pathSource) |> Utility.sha1CheckSum' 
      | false -> ""
    let hashTarget = 
      match File.Exists(pathTarget) with 
      | true -> File.ReadAllBytes(pathTarget) |> Utility.sha1CheckSum' 
      | false -> ""

    match hashSource = hashTarget with
    | true -> ""
    | false -> sprintf "<tr><td>%s</td><td>%s</td></tr>" hashSource hashTarget  


  let textToXml (path:string) = 
    let xmlPath = path + ".xml"
    use writer = XmlWriter.Create(xmlPath)
    writer.WriteStartDocument()
    writer.WriteStartElement("file")
    
    match File.Exists(path) with
      | true -> 
        File.ReadLines(path)
        |> Seq.iter(fun x -> 
          writer.WriteCData(x))
      | false -> ()

    writer.WriteEndElement()
    writer.WriteEndDocument()
    writer.Close()
    xmlPath

  let htmlXmlDiff (pathSource : string) xmlDiff (log:ConsoleLogger) =
    use sr = new StringReader(xmlDiff)
    use diffFile = new XmlTextReader(sr)
    use sourceFile = new XmlTextReader(pathSource)
    use mm = new MemoryStream()
    use sw = new StreamWriter(mm)

    try
      try
        let diffView = new XmlDiffView()

        diffView.Load(sourceFile, diffFile)
        diffView.GetHtml(sw)

        // Works for small .XML (needs to be optimized for bigger)
        System.Text.Encoding.UTF8.GetString(changeColourScheme(mm.ToArray()))
          .Replace("&lt;?xml version=\"1.0\" encoding=\"utf-8\"?&gt;","");
      with ex -> log.WriteLine(LogLevel.Error,sprintf "%s" ex.Message ); String.Empty
    finally
      sw.Flush()
      sw.Close()
      sourceFile.Close()
      diffFile.Close()

  // Produced a string containing xml with the diff of two packaged files
  let diff pathSource pathTarget (log:ConsoleLogger) =
    let diff' = XmlDiff(diffOptions ())
    do diff'.Algorithm <- XmlDiffAlgorithm.Precise

    use sw = new StringWriter()
    use xtw = new XmlTextWriter(sw)
    do xtw.Formatting <- Formatting.Indented 

    try
      try
        match diff'.Compare(pathSource, pathTarget, true, xtw) with
        | false -> sw.ToString()
        | true -> String.Empty
      with ex -> log.WriteLine(LogLevel.Error,sprintf "%s" ex.Message ); String.Empty
    finally
      xtw.Close()
      sw.Close()

  // Produces a sequence containing the parts that are changed, unchanged, 
  // not in source and not in target of two packaged solutions.
  // The output files 
  let diffs pathSource pathTarget (log:ConsoleLogger.ConsoleLogger) =

    let tmpSource = unpackSolution pathSource log
    let tmpTarget = unpackSolution pathTarget log

    let source = 
      Directory.EnumerateFiles(tmpSource, @"*.*",  SearchOption.AllDirectories)
      |> Seq.filter(fun f -> not (f.EndsWith(".data.xml")))
      |> Seq.sort
      |> Seq.map(fun f -> f.[f.IndexOf(tmpSource) + 1 + tmpSource.Length ..])
      |> Set.ofSeq
    let target = 
      Directory.EnumerateFiles(tmpTarget, @"*.*", SearchOption.AllDirectories)
      |> Seq.filter(fun f -> not (f.EndsWith(".data.xml")))
      |> Seq.sort
      |> Seq.map(fun f -> f.[f.IndexOf(tmpTarget) + 1 + tmpTarget.Length ..])
      |> Set.ofSeq

    let notTarget = source - target
    let notSource = target - source
    let intersect = Set.intersect source target 

    let notTarget' = 
      notTarget
      |> Set.toArray
      |> Array.Parallel.map(fun f -> 
        
        let fs = Path.Combine(tmpSource,f)
        let ft = Path.Combine(tmpTarget,f)

        Path.GetDirectoryName(ft) |> Utility.ensureDirectory

        match f with
        | XML ->
          let root = XDocument.Load(uri = fs).Root.Name
          (new XDocument(new XElement(root))).Save(ft)

          f, htmlXmlDiff fs (diff fs ft log) log, DiffType.NotInTarget
        | Text ->
          let fs' = textToXml fs
          let ft' = textToXml ft
          let htmlDiff = 
            (htmlXmlDiff fs' (diff fs' ft' log) log)
            |> cleanHtmlTextDiff
          f, htmlDiff, DiffType.NotInTarget
        | Binary | _ ->  
          f, htmlBinaryShaDiff fs ft, DiffType.NotInTarget)

    let notSource' = 
      notSource
      |> Set.toArray
      |> Array.Parallel.map(fun f -> 
        
        let fs = Path.Combine(tmpSource,f)
        let ft = Path.Combine(tmpTarget,f)

        Path.GetDirectoryName(fs) |> Utility.ensureDirectory

        match f with
        | XML ->
          let root = XDocument.Load(uri = ft).Root.Name
          (new XDocument(new XElement(root))).Save(fs)

          f, htmlXmlDiff fs (diff fs ft log) log, DiffType.NotInSource
        | Text ->
          let fs' = textToXml fs
          let ft' = textToXml ft
          let htmlDiff = 
            (htmlXmlDiff fs' (diff fs' ft' log) log)
            |> cleanHtmlTextDiff
          f, htmlDiff, DiffType.NotInSource
        | Binary | _ ->  
          f, htmlBinaryShaDiff fs ft, DiffType.NotInSource)

    let intersect' = 
      intersect
      |> Set.toArray
      |> Array.Parallel.map(fun f -> 

        let fs = Path.Combine(tmpSource,f)
        let ft = Path.Combine(tmpTarget,f)

        let shas = File.ReadAllBytes(fs) |> Utility.sha1CheckSum'
        let shat = File.ReadAllBytes(ft) |> Utility.sha1CheckSum'

        match f with
        | XML ->
          let htmlDiff' =
            match shas = shat with
            | true -> String.Empty
            | false -> 
              let diff' = (diff fs ft log)

              match String.IsNullOrEmpty(diff') with
              | true -> String.Empty
              | false -> htmlXmlDiff fs diff' log
          f,htmlDiff'
        | Text ->
          let htmlDiff' =
            match shas = shat with
            | true -> String.Empty
            | false -> 
              let fs' = textToXml fs
              let ft' = textToXml ft
              let diff' = (diff fs' ft' log)

              match String.IsNullOrEmpty(diff') with
              | true -> String.Empty
              | false -> 
                (htmlXmlDiff fs' (diff fs' ft' log) log)
                |> cleanHtmlTextDiff
          f,htmlDiff'
        | Binary | _ ->  
          let htmlDiff' =
            match shas = shat with
            | true -> String.Empty
            | false -> htmlBinaryShaDiff fs ft
          f, htmlDiff')

    let unchanged =
      intersect'
      |> Array.filter(fun (_,data) -> String.IsNullOrEmpty(data))
      |> Array.map(fun (f,data) -> (f,data,DiffType.Unchanged))

    let changed = 
      intersect'
      |> Array.filter(fun (_,data) -> not (String.IsNullOrEmpty(data)))
      |> Array.map(fun (f,data) -> (f,data,DiffType.Changed))

    Directory.Delete(tmpSource,true)
    Directory.Delete(tmpTarget,true)

    seq{ 
      yield! unchanged; 
      yield! changed; 
      yield! notSource'; 
      yield! notTarget'; } 

  // Module for constructing a walkable tree datastructure of the diff
  module DiffTree =
    type Tree =
          | Node of string * string * Tree list
          | Leaf of string * string * string * DiffType

    let addToNode (node:Tree) b =
      match node with
      | Node(id,name,list) -> Node(id,name,List.append list b)
      | leaf ->  leaf

    let getBranches = function
      | Node(_,_,list) -> list
      | _ ->  []

    let rec getChildTypeCount diffType = function
      | Node(_,_,branches)-> 
          branches |> List.fold(fun acc x -> acc + getChildTypeCount diffType x) 0
      | Leaf(_,_,_,leafType) when leafType = diffType -> 1
      | Leaf(_,_,_,_) -> 0

    // Recursive function for building the diff tree
    // takes in a sequence of tuples containing 
    // the file path in the package, list of the parent folders, 
    // the content of the difference in the file and the difference type
    let treeDataBuild input =
        let rec loop acc parentName inp =
          // Uses the root folder name as key to group the subfolder/files together
          let inp' =
            inp
            |> Seq.groupBy(fun (_,header,_,_) ->
                match header with
                  | [] -> String.Empty
                  | hd -> List.head hd)
            |> Seq.toList

          // If the input is not empty then collect the files by their root 
          // folder
          match inp' with
            | [] -> acc
            | y ->
              y |> List.collect(fun (key,t) -> 

                // Goes trough the grouped folders and construct a new node or leaf
                match t |> Seq.toList with
                  // No children.
                  | [] -> []
                  // One child. If no children then it is a leaf else 
                  // call loop and parse in the child
                  | (id,header,data,diffType)::[] ->  
                    match header with
                    | [ ] -> [ ]
                    | [x] -> [Tree.Leaf(id,key,data,diffType)] 
                    | x :: xs -> 
                      let newName = (parentName + key + "\\")
                      [ [(id,xs,data,diffType)] 
                        |> List.toSeq
                        |> loop (Tree.Node(newName,key,List.empty)) newName ]
                  // Multiple children. Call loop and parse in a seq of childrens
                  | (id,header,data,diffType)::xs as zs -> 
                    let newName = (parentName + key + "\\")
                    [(id,header,data,diffType)::xs
                      |> List.map(fun (id,t,d,diffType) -> 
                          match t with
                            | [] -> None
                            | z :: zs ->  Some (id,zs,d,diffType))
                      |> List.choose (fun x -> x)
                      |> List.toSeq
                      |> loop (Tree.Node(newName,key,List.empty)) newName ])
              |> addToNode acc
        loop (Tree.Node("Root","Root",List.empty)) "" input


    // Walks through the constructed tree and produce html for the first row of 
    // the display table that contains the name of the Node/Leaf and with expand link
    let rec treeHtmlWalkerNameRow (sw:StreamWriter) spaces = function
        | Leaf(id,name,data,diffType) ->
          match String.IsNullOrWhiteSpace(data) with
            | false -> 
              let callId = id.Replace(@"|", @"") |> urlEncodeUpper
              sprintf "%s<li><a class='leaf %s' href=\"javascript:ajaxhtmlDiff('%s','%s');\" id='dg_%s'>%s</a></li>" 
                spaces (diffType.ToString()) name (callId) id (name.Replace(@"|", @"\"))
              |> sw.WriteLine
            | true -> 
              sprintf "%s<li><a class='leaf inactivelink %s'>%s</a></li>" 
                spaces (diffType.ToString()) (name.Replace(@"|", @"\"))
              |> sw.WriteLine

        | Node(_,name,branches) -> 
          sprintf "%s<li><a class='Collapsable underline' tag='%s'>%s</a><ul style='display: none;'>"
            spaces name name
          |> sw.WriteLine

          branches
          |> List.iter(fun x -> treeHtmlWalkerNameRow sw (sprintf "\t%s" spaces) x)
          sw.WriteLine(spaces+"</ul>")
          sw.WriteLine(spaces+"</li>")
          sw.Flush()

    // Walks through the constructed tree and produce html for the other rows 
    // containing the difftype of the row and display the number of children in a node
    let rec treeHtmlTypeWalkerTypeRows (sw:StreamWriter) diffType spaces = function
        | Leaf(_,_,_,_) -> 
          sw.WriteLine(spaces + "<li> &nbsp; </li>")
        | Node(id,name,branches) -> 
          let count = 
            match  getChildTypeCount diffType (Tree.Node(id,name,branches)) with
            | 0 -> "&nbsp;"
            | x -> x.ToString()
          sprintf "%s<li><a tag='%s' class='underline inactivelink'>%s</a><ul class='noIndent' style='display: none;'>" 
            spaces name count
          |> sw.WriteLine

          branches
          |> List.iter(fun x -> treeHtmlTypeWalkerTypeRows sw diffType (sprintf "    %s" spaces) x)
          sw.WriteLine(spaces+"</ul>")
          sw.WriteLine(spaces+"</li>")
          sw.Flush()

  //Produces the a table which displays the diff three
  let diffBlock (ds:seq<string*string*DiffType>) spaces =
    
    use ms = new MemoryStream()
    use sw = new StreamWriter(ms) 

    let ds' = 
      ds
      |> Seq.map(fun (name,data,diffType) -> 
        let name' = 
          name
            .Replace(@"{", @"")
            .Replace(@"}", @"")
            .Replace(@"\", @"|")
            .Replace(@" ", @"")
        (name',(name'.Split([|'|'|]) |> Array.toList),data,diffType))
      |> DiffTree.treeDataBuild

    sw.WriteLine(spaces + "<td class='row-name'>")

    // First row
    DiffTree.getBranches ds'
    |> List.iter(fun t -> 

      sw.WriteLine(spaces + "    <ul>")  
      DiffTree.treeHtmlWalkerNameRow sw (spaces + "\t")  t
      sw.WriteLine(spaces + "    </ul>")
      sw.Flush())
    
    sw.WriteLine(spaces + "</td>")
    
    // Second-Fifth row
    [DiffType.Unchanged; DiffType.Changed; DiffType.NotInSource; DiffType.NotInTarget]
    |> List.iter(fun diffT -> 
      sw.WriteLine(spaces + "<th class='row-diff'>")

      DiffTree.getBranches ds'
      |> List.iter(fun t -> 

        sw.WriteLine(spaces + "    <ul>")  
        DiffTree.treeHtmlTypeWalkerTypeRows sw diffT (spaces + "\t") t
        sw.WriteLine(spaces + "    </ul>"))

      sw.WriteLine(spaces + "</th>")
      sw.Flush())
    
    System.Text.Encoding.UTF8.GetString(ms.ToArray())

  // Produces a WebPart used for Suave 
  let app pathSource pathTarget ds = 

    let getSolutionName (path:string) =
      let path' = path.Split([|'\\'|])
      path'.[(Array.length path')-1]

    let html = 
      (txtResources.["diff.html"])
        .Replace(@"{{diffBlock}}",diffBlock ds "\t\t\t")
        .Replace(@"{{solutionSource}}",getSolutionName pathSource)
        .Replace(@"{{solutionTarget}}",getSolutionName pathTarget)
        .Replace(@"{{assemblyVersion}}",Utility.assemblyVersion)
        .Replace(@"{{timeStamp}}",Utility.timeStamp())

    // Produces the link for ajax call to fetch the diff content of a file
    let treeLinks = 
      ds 
      |> Seq.filter(fun (_,data,_) -> not (String.IsNullOrWhiteSpace(data)))
      |> Seq.map(fun (name,data,_) ->
        let name' = 
          name
            .Replace(@"{", @"")
            .Replace(@"}", @"")
            .Replace(@"\", @"")
            .Replace(@" ", @"")
          |> urlEncodeUpper

        path ("/Daxif/diff/view/id_" + name') >=> (OK data))
      |> Seq.toList
      |> choose
        
    GET >=> choose 
      [ path "/Daxif/diff" >=> OK html
        treeLinks
        path "/Daxif/diff/diffStyle.css" >=> Writers.setMimeType "text/css" >=> OK txtResources.["diffStyle.css"]
        path "/Daxif/diff/diff.js" >=> Writers.setMimeType "application/javascript" >=> OK txtResources.["diff.js"]
        path "/favicon.ico" >=> Writers.setMimeType "application/javascript" >=> Web.binaryWebPart.["favicon.ico"]
        path "/close" >=> CLOSE
        RequestErrors.NOT_FOUND "Found no handlers"]

  let solutionApp pathSource pathTarget (log:ConsoleLogger.ConsoleLogger) =
    diffs pathSource pathTarget log
    |> app pathSource pathTarget

  // Creates the app for displaying the diff page and start the webserver and 
  // opens the web page in the default webbrowser 
  let solution' pathSource pathTarget log =

    // Check that the files exist
    [pathSource ; pathTarget]
    |> List.iter(fun path -> 
      match File.Exists path with
      | true -> ()
      | false -> failwith (sprintf "Could not find file %s" path))

    let app' = solutionApp pathSource pathTarget log

    let port = 
      availablePort

    let cfg =
      { defaultConfig with
          bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ]
      }  

    // Opens default webbrowser with the solutiondiff html page
    System.Diagnostics.Process.Start( sprintf "http://localhost:%s" (port.ToString()) + "/Daxif/diff") |> ignore;
    startWebServer cfg app'


  // Module for producing a csv version of the diff
  module Summary =
    let rec nameTreeWalker (sw:StreamWriter) = function
          | DiffTree.Leaf(id,_,_,diffType) ->
            match diffType.GetHashCode() with
            | 0 | 1 -> sw.Write(id + ";" + id)
            | 2 -> sw.Write(";" + id)
            | 3 -> sw.Write(id + ";")
            | _ -> ()
            sw.Write(";;;;" + sw.NewLine)
          | DiffTree.Node(id,name,branches) -> 
          
            sw.Write(id + ";" + id + ";")
          
            let rec typeCountWriter = function
              | x::[] -> 
                let c = DiffTree.getChildTypeCount x (DiffTree.Node(id,name,branches))
                sw.Write(c.ToString() + sw.NewLine)
              | x::xs -> 
                let c = DiffTree.getChildTypeCount x (DiffTree.Node(id,name,branches))
                sw.Write(c.ToString() + ";")
                typeCountWriter xs
              | [] -> ()
            
            [DiffType.Unchanged; DiffType.Changed; DiffType.NotInSource; DiffType.NotInTarget]
            |> typeCountWriter

            branches
            |> List.iter(fun x -> nameTreeWalker sw x)

    let build pathSource pathTarget (log:ConsoleLogger.ConsoleLogger) = 
      // Check that the files exist
      [pathSource ; pathTarget]
      |> List.iter(fun path -> 
        match File.Exists path with
        | true -> ()
        | false -> failwith (sprintf "Could not find file %s" path))

      let root = executingPath
      let csvPath = Path.Combine(root,"diffSummary.csv")

      Path.GetDirectoryName(csvPath) |> Utility.ensureDirectory

      use fs' = File.Create(csvPath)
      use sw = new StreamWriter(fs') 
      sw.WriteLine("sep=;")
      sw.WriteLine("Source;Target;Changed;Unchanged;Not in source;Not in target")

      diffs pathSource pathTarget log
      |> fun ds ->

        let ds' = 
          ds
          |> Seq.map(fun (name,data,diffType) -> 
            let name' = 
              name
                .Replace(@"{", @"")
                .Replace(@"}", @"")
                .Replace(@" ", @"")

            (name',(name'.Split([|'\\'|]) |> Array.toList),data,diffType))
          |> DiffTree.treeDataBuild

        DiffTree.getBranches ds'
        |> List.iter(fun t -> 
          nameTreeWalker sw t)
      csvPath