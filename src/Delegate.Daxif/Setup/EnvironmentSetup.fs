namespace DG.Daxif

open System
open System.Collections.Generic
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open DG.Daxif.Setup
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

/// Used to get new proxy connections to a CRM environment
type Connection = {
  serviceManagement: IServiceManagement<IOrganizationService>
  authCreds: AuthenticationCredentials 
} with
  static member Connect(org, ap, usr, pwd, dmn) =
    let m, at = CrmAuth.authenticate org ap usr pwd dmn
    { serviceManagement = m; authCreds = at }

  member x.GetProxy() = CrmAuth.getOrganizationServiceProxy x.serviceManagement x.authCreds


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
 
  static member Get(name) = EnvironmentHelper.get name
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
     
    let env = {
      name = name
      url = Uri(url)
      creds = credsToUse
      ap = ap
    }

    EnvironmentHelper.add name env
    env
  
  member x.apToUse = x.ap ?| AuthenticationProviderType.OnlineFederation

  member x.getCreds() = 
    match x.creds with
    | None   -> CredentialManagement.getCredentials x.name
    | Some c -> c.getValues()
    

  member x.connect(?logger: ConsoleLogger) =
    let usr, pwd, dmn = x.getCreds()

    // Log connection info if logger provided
    logger ?|>+ (fun log ->
      log.Info "Environment: %O" x
      log.Info "User: %s" usr
    )

    try
      Connection.Connect(x.url, x.apToUse, usr, pwd, dmn)
    with 
      ex -> 
        logger ?|>+ (fun log -> log.Error "Unable to connect to CRM.")
        // Retry if credentials were stored locally
        match x.creds ?>> fun c -> c.key ?>> CredentialManagement.promptNewCreds with
        | Some newCreds -> 
          match logger with
          | None -> x.connect()
          | Some log -> 
            log.Info "Reconnecting with new credentials."
            x.connect(log)
              
        | _ -> raise ex

  
and internal EnvironmentHelper private() =

  static let envDict = Dictionary<string, Environment>()

  static member add name env = 
    match envDict.ContainsKey name with
    | true  -> envDict.Remove(name) |> ignore
    | false -> ()
    envDict.Add(name, env)
    
  static member get name = 
    let found, env = envDict.TryGetValue(name)
    match found with
    | true  -> env
    | false -> 
      let availableNames = envDict.Keys |> Seq.map (sprintf "'%s'") |> String.concat ", "
      failwithf "Unable to find environment with name '%s'. Available names are: %s" name availableNames
          
          