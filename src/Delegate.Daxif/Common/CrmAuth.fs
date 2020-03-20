module internal DG.Daxif.Common.CrmAuth

open System.Net
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open DG.Daxif.Common.InternalUtility
open System
open Microsoft.Xrm.Tooling.Connector
open System.IO


// Get credentials based on provider, username, password and domain
let internal getCredentials provider username (password: string) domain =

  let ac = AuthenticationCredentials()

  match provider with
  | AuthenticationProviderType.ActiveDirectory ->
      ac.ClientCredentials.Windows.ClientCredential <-
        NetworkCredential(username, password, domain)

  | AuthenticationProviderType.OnlineFederation -> // CRM Online using Office 365 
      ac.ClientCredentials.UserName.UserName <- username
      ac.ClientCredentials.UserName.Password <- password

  | AuthenticationProviderType.Federation -> // Local Federation
      ac.ClientCredentials.UserName.UserName <- username
      ac.ClientCredentials.UserName.Password <- password

  | _ -> failwith "No valid authentification provider was used."

  ac

// Get Organization Service Proxy
let internal getOrganizationServiceProxy
  (serviceManagement:IServiceManagement<IOrganizationService>)
  (authCredentials:AuthenticationCredentials) =
  let ac = authCredentials

  let proxy =
    match serviceManagement.AuthenticationType with
    | AuthenticationProviderType.ActiveDirectory ->
        new OrganizationServiceProxy(serviceManagement, ac.ClientCredentials)
    | _ ->
        new OrganizationServiceProxy(serviceManagement, ac.SecurityTokenResponse)

  let timeOut = TimeSpan(0,59,0)
  log.Verbose @"Connection timeout set to %i hour, %i minutes, %i seconds" timeOut.Hours timeOut.Minutes timeOut.Seconds
  proxy.Timeout <- timeOut
  proxy

// Authentication
let authenticate org ap username password domain =
  let m = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(org)
  let at = m.Authenticate(getCredentials ap username password domain)
  m,at

let ensureClientIsReady (client: CrmServiceClient) =
  match client.IsReady with
  | false ->
    let s = sprintf "Client could not authenticate. If the application user was just created, it might take a while before it is available.\n%s" client.LastCrmError 
    in failwith s
  | true -> client

let internal getCrmServiceClient userName password (orgUrl:Uri) mfaAppId mfaReturnUrl =
  let mutable orgName = ""
  let mutable region = ""
  let mutable isOnPrem = false
  Utilities.GetOrgnameAndOnlineRegionFromServiceUri(orgUrl, &region, &orgName, &isOnPrem)
  let cacheFileLocation = System.IO.Path.Combine(System.IO.Path.GetTempPath(), orgName, "oauth-cache.txt")
  let timeOut = TimeSpan(0,59,0)
  log.Verbose @"Connection timeout set to %i hour, %i minutes, %i seconds" timeOut.Hours timeOut.Minutes timeOut.Seconds
  CrmServiceClient.MaxConnectionTimeout <- timeOut
  new CrmServiceClient(userName, CrmServiceClient.MakeSecureString(password), region, orgName, false, null, null, mfaAppId, Uri(mfaReturnUrl), cacheFileLocation, null)
  |> ensureClientIsReady

let internal getCrmServiceClientClientSecret (org: Uri) appId clientSecret =
  let timeOut = TimeSpan(0,59,0)
  log.Verbose @"Connection timeout set to %i hour, %i minutes, %i seconds" timeOut.Hours timeOut.Minutes timeOut.Seconds
  CrmServiceClient.MaxConnectionTimeout <- timeOut
  new CrmServiceClient(org, appId, CrmServiceClient.MakeSecureString(clientSecret), true, Path.Combine(Path.GetTempPath(), appId, "oauth-cache.txt"))
  |> ensureClientIsReady