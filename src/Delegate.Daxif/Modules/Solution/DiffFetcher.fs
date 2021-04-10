module DG.Daxif.Modules.Solution.DiffFetcher

open DG.Daxif.Common
open InternalUtility
open System.IO
open System.IO.Compression
open System
open Microsoft.Xrm.Sdk.Query
open System.Threading
open System.Xml
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Crm.Sdk.Messages

let selectNodes (node: XmlNode) (xpath: string) =
  node.SelectNodes(xpath)
  |> Seq.cast<XmlNode>

let fetchSolution proxy (solution: string) = 
  let columnSet = ColumnSet("uniquename", "friendlyname", "publisherid", "version")
  CrmDataInternal.Entities.retrieveSolution proxy solution columnSet

let downloadSolution (env: DG.Daxif.Environment) file_location sol_name async (timeOut: TimeSpan) =
  log.Verbose "Exporting extended solution %A" (file_location + sol_name)
  SolutionHelper.exportWithExtendedSolution env sol_name file_location false async timeOut

let unzip file =
  log.Verbose "Unpacking zip '%s' to '%s'" (file + ".zip") file;
  if Directory.Exists(file) then
    Directory.Delete(file, true) |> ignore;
  ZipFile.ExtractToDirectory(file + ".zip", file);

let fetchEntityAllMetadata (proxy: IOrganizationService) (name: string) =
  let req = RetrieveEntityRequest ()
  req.EntityFilters <- EntityFilters.All;
  req.LogicalName <- name.ToLower();
  proxy.Execute(req) :?> RetrieveEntityResponse

let fetchSolutionComponents proxy (solutionid: Guid) =
  let query = QueryExpression("solutioncomponent")
  query.ColumnSet <- ColumnSet("componenttype", "objectid");
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionid.ToString("B"));
  CrmDataHelper.retrieveMultiple proxy query

let fetchSitemapId (proxy: IOrganizationService) (unique_name: string) = 
  let query = QueryExpression("sitemap")
  query.ColumnSet <- ColumnSet(false);
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("sitemapnameunique", ConditionOperator.Equal, unique_name);
  CrmDataHelper.retrieveMultiple proxy query
  |> Seq.head
  |> fun a -> a.Id

let fetchAppModuleId (proxy: IOrganizationService) (unique_name: string) = 
  let query = QueryExpression("appmodule")
  query.ColumnSet <- ColumnSet(false);
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, unique_name);
  CrmDataHelper.retrieveMultiple proxy query
  |> Seq.head
  |> fun a -> a.Id

let fetchImportStatusOnce proxy (id: Guid) =
  let query = QueryExpression("importjob")
  query.NoLock <- true;
  query.ColumnSet <- ColumnSet("progress", "completedon", "data");
  query.Criteria <- FilterExpression();
  query.Criteria.AddCondition("importjobid", ConditionOperator.Equal, id);
  CrmDataHelper.retrieveMultiple proxy query
  |> Seq.tryHead
  |> Option.map (fun a -> 
    a.Attributes.Contains("completedon"), 
    a.Attributes.["progress"] :?> double, 
    a.Attributes.["data"] :?> string)

let fetchWorkflowIds proxy (solutionid: Guid) =
  let le = LinkEntity()
  le.JoinOperator <- JoinOperator.Inner;
  le.LinkFromAttributeName <- @"workflowid";
  le.LinkFromEntityName <- @"workflow";
  le.LinkToAttributeName <- @"objectid";
  le.LinkToEntityName <- @"solutioncomponent";
  le.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solutionid);
  let q = QueryExpression("workflow")
  q.LinkEntities.Add(le);
  q.Criteria <- FilterExpression ();
  q.Criteria.AddCondition("type", ConditionOperator.Equal, 1);
  CrmDataHelper.retrieveMultiple proxy q
  |> Seq.toList
  |> List.map (fun e -> e.Id)

let fetchGlobalOptionSet (proxy: IOrganizationService) name =
  let req = RetrieveOptionSetRequest ()
  req.Name <- name;
  let resp = proxy.Execute(req) :?> RetrieveOptionSetResponse
  let oSetMeta = resp.OptionSetMetadata :?> OptionSetMetadata
  oSetMeta.Options
  |> Seq.map (fun o -> (o.Value.Value, o.Label.UserLocalizedLabel.Label))
  |> Map.ofSeq

let fetchComponentTypes proxy = fetchGlobalOptionSet proxy "componenttype"

let rec fetchImportStatus proxy id asyncid =
  Thread.Sleep 10000;
  match fetchImportStatusOnce proxy id with
    | None -> 
      log.Verbose "Import not started yet.";
      fetchImportStatus proxy id asyncid
    | Some (_, _, data) when data.Contains("<result result=\"failure\"") -> 
      let job = CrmData.CRUD.retrieve proxy "asyncoperation" asyncid
      if job.Attributes.ContainsKey "message" then
        log.Verbose "%s" (job.Attributes.["message"] :?> string);
      else 
        let doc = XmlDocument ()
        doc.LoadXml data;
        selectNodes doc "//result[@result='failure']"
        |> Seq.iter (fun a -> log.Verbose "%s" (a.Attributes.GetNamedItem "errortext").Value)
      failwithf "An error occured in import.";
    | Some (true, _, _) -> 
      log.Info "Import succesful.";
    | Some (false, progress, _) -> 
      log.Verbose "Importing: %.1f%%" progress;
      fetchImportStatus proxy id asyncid