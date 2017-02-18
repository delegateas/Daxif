namespace DG.Daxif

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open DG.Daxif.Common
open DG.Daxif.Setup
open Utility

/// Used to get new proxy connections to a CRM environment
type Connection = {
  serviceManagement: IServiceManagement<IOrganizationService>
  authCreds: AuthenticationCredentials 
} with
  static member Connect(org, ap, usr, pwd, dmn) =
    let m, at = CrmAuth.authenticate org ap usr pwd dmn
    { serviceManagement = m; authCreds = at }

  member x.GetProxy() = CrmAuth.proxyInstance x.serviceManagement x.authCreds


/// Manages credentials used for connecting to a CRM environment
type Credentials = {
  key: string option
  username: string option
  password: string option
  domain: string option
} with
  static member None = Credentials.Create()
  static member FromKey(key) =
    { 
      key = Some key
      username = None
      password = None
      domain = None
    }

  static member Create(?username, ?password, ?domain) =
    { 
      key = None
      username = username
      password = password
      domain = domain 
    }

  member x.getValues() =
    match x.key with
    | Some k -> CredentialManagement.getCredentials k
    | None ->
      x.username ?| "",
      x.password ?| "",
      x.domain ?| ""



/// Describes a connection to a Dynamics CRM/365 environment
type Environment = {
  name: string
  url: Uri
  creds: Credentials option
  ap: AuthenticationProviderType option
} with 
  override x.ToString() = sprintf "%A (%A)" x.name x.url

  static member Create(name, url, ?ap, ?creds, ?args) =
    let credsToUse = 
      match args ?|> parseArgs with
      | None        -> creds
      | Some argMap ->
        let usr = tryFindArg ["username"; "usr"; "u"] argMap ?| ""
        let pwd = tryFindArg ["password"; "pwd"; "p"] argMap ?| "" 
        let dmn = tryFindArg ["domain";   "dmn"; "d"] argMap ?| ""
        match (usr + pwd + dmn).Length > 0 with
        | true  -> Credentials.Create(usr, pwd, dmn) |> Some
        | false -> creds

    {
      name = name
      url = Uri(url)
      creds = credsToUse
      ap = ap
    }
  
  member x.apToUse = x.ap ?| AuthenticationProviderType.OnlineFederation

  member x.getCreds() = 
    match x.creds with
    | None   -> CredentialManagement.getCredentials x.name
    | Some c -> c.getValues()
    

  member x.connect(?logger: ConsoleLogger) =
    let usr, pwd, dmn = x.getCreds()

    // Log connection info if logger provided
    match logger with
    | None -> ()
    | Some l -> 
      l.Info "Environment: %O" x
      l.Info "User: %s" usr

    Connection.Connect(x.url, x.apToUse, usr, pwd, dmn)
