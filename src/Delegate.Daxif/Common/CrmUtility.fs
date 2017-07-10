module internal DG.Daxif.Common.CrmUtility

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Query
open Microsoft.Xrm.Sdk.Client
open Microsoft.Crm.Sdk.Messages
open Microsoft.Xrm.Sdk.Messages

open System.IO
open System.IO.Compression
open System.Xml
open System.Xml.Linq
open System.Xml.XPath


/// Tries to fetch an attribute. If the attribute does not exist,
/// a default value is returned.
let defaultAttributeVal (e:Entity) key (def:'a) =
  match e.Attributes.TryGetValue(key) with
  | (true,v) -> v :?> 'a
  | (false,_) -> def

/// Gets the integer value from an OptionSetValue attribute, or default if null
let getDefaultOptSetValue (e: Entity) attr def =
  let v = e.GetAttributeValue<OptionSetValue>(attr)
  if isNull v then def else v.Value

/// Check if the Guid is empty
let guidNotSet = (=) Guid.Empty

/// Fetches the name of an entity
let getRecordName (x:Entity) = x.GetAttributeValue<string>("name")

/// Upcasts a request to an OrganizationRequest
let toOrgReq x = x :> OrganizationRequest

/// Attaches a given request to a solution
let attachToSolution solutionName (req: #OrganizationRequest) = 
  req.Parameters.Add("SolutionUniqueName", solutionName)
  req

/// Parses a fault to a list of strings
let rec parseFault (fault: OrganizationServiceFault) =
  let lines = 
    [ sprintf "%A: %s" fault.Timestamp fault.Message
      sprintf "Errorcode: %d" fault.ErrorCode
      sprintf "Trace:\n%s" fault.TraceText
    ]
  match fault.InnerFault with
  | null  -> lines
  | inner -> lines @ ("" :: "\t[======INNER FAULT======]" :: parseFault inner)

/// Raises an exception if faul is not null
let raiseExceptionIfFault (fault: OrganizationServiceFault) =
  match fault with
  | null -> ()
  | err -> parseFault fault |> String.concat "\n" |> failwith


/// Gets solution information from given ZipArchive
let getSolutionInformation (archive: ZipArchive) =
  match archive.Entries |> Seq.exists(fun e -> e.Name = "solution.xml") with
  | false -> 
    failwith "Invalid CRM package. solution.xml file not found"
  | true ->
    let solutionDoc = 
      archive.GetEntry("solution.xml")
      |> fun entry -> XDocument.Load(entry.Open())

    let solutionNameNode = 
      solutionDoc.XPathSelectElement("/ImportExportXml/SolutionManifest/UniqueName");
    let managedNode = 
      solutionDoc.XPathSelectElement("/ImportExportXml/SolutionManifest/Managed");
    
    match solutionNameNode.IsEmpty || managedNode.IsEmpty with
      | true  -> failwith "Invalid CRM package. Solution name or managed setting not found in solution package."
      | false -> solutionNameNode.Value, managedNode.Value = "1"


/// Gets solution information from zip file found at given path
let getSolutionInformationFromFile path = 
  use zipStream = new FileStream(path, FileMode.Open)
  use archive = new ZipArchive(zipStream, ZipArchiveMode.Read)
  getSolutionInformation archive


/// Takes a QueryExpression and outputs it as a formatted string
let queryExpressionToString (query: QueryExpression) = 

  let rec filterHelper level (filter: FilterExpression) = seq {
    let indent = String.replicate level "\t"

    yield sprintf "%s%A(" indent filter.FilterOperator
    yield!
      filter.Conditions 
      |> Seq.map (fun c -> sprintf "\t%s%s %A %A" indent c.AttributeName c.Operator c.Values)
    
    yield!
      filter.Filters 
      |> Seq.map (filterHelper (level+1))
      |> Seq.concat
    
    yield sprintf "%s)" indent
  }
  
  let filter = filterHelper 1 query.Criteria |> List.ofSeq

  sprintf "LogicalName: %s" query.EntityName
  :: sprintf "Columns: %A" query.ColumnSet
  :: sprintf "Filter: "
  :: filter
  |> String.concat "\n"