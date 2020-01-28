namespace DG.Daxif

open System
open System.Collections.Generic
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open DG.Daxif.Setup
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

type ConnectionMethod = 
  | Proxy of IServiceManagement<IOrganizationService> * AuthenticationCredentials 
  | CrmServiceClient of Uri * string * string * string * string

/// Used to get new proxy connections to a CRM environment
type Connection = {
  method: ConnectionMethod
} with

  /// Creates a connection with the given credentials
  static member Connect(org, ap, usr, pwd, dmn) =
    let m, at = CrmAuth.authenticate org ap usr pwd dmn
    { method = ConnectionMethod.Proxy(m,at) }
  
  static member Connect(org,usr,pwd,appId,returnUrl) =
    { method = ConnectionMethod.CrmServiceClient(org,usr,pwd,appId,returnUrl)}

  /// Connects to the environment and returns an IOrganizationService
  member x.GetProxy() = 
    match x.method with
    | ConnectionMethod.Proxy(m,at) -> 
      CrmAuth.getOrganizationServiceProxy m at :> IOrganizationService
    | ConnectionMethod.CrmServiceClient(org,usr,pwd,appId,returnUrl) ->
      CrmAuth.getOrganizationServiceProxyUsingMFA usr pwd org appId returnUrl


/// Manages credentials used for connecting to a CRM environment
type Credentials = {
  key: string option
  username: string option
  password: string option
  domain: string option
} with
  static member None = Credentials.Create()

  /// Gets credentials from a given identification key
  static member FromKey(key) =
    { 
      key = Some key
      username = None
      password = None
      domain = None
    }
  
  /// Constructs credential with the given username, password, and domain
  static member Create(?username, ?password, ?domain) =
    { 
      key = None
      username = username
      password = password
      domain = domain 
    }

  /// Gets the raw credential values from this credential key
  member x.getValues() =
    match x.key with
    | Some k -> CredentialManagement.getCredentialsFromKey k
    | None ->
      x.username ?| "",
      x.password ?| "",
      x.domain ?| ""


/// Describes a connection to a Dynamics 365/CRM environment
type Environment = {
  name: string
  url: Uri
  creds: Credentials option
  ap: AuthenticationProviderType option
  mfaAppId: string option
  mfaReturnUrl: string option
} with 
  override x.ToString() = sprintf "%A (%A)" x.name x.url
 
  /// Gets the environment with the given name, if one exists
  static member Get(name) = EnvironmentHelper.get name

  /// Creates a new environment using the credentials and arguments given
  static member Create(name, url, ?ap, ?creds, ?mfaAppId, ?mfaReturnUrl, ?args) =
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
      mfaAppId = mfaAppId
      mfaReturnUrl = mfaReturnUrl
    }

    EnvironmentHelper.add name env
    env
  
  member x.apToUse = x.ap ?| AuthenticationProviderType.OnlineFederation

  /// Gets credentials for the given environment
  member x.getCreds() = 
    match x.creds with
    | None   -> CredentialManagement.getCredentialsFromKey x.name
    | Some c -> c.getValues()
    

  /// Connects to the given environment
  member x.connect(?logger: ConsoleLogger) =
    let usr, pwd, dmn = x.getCreds()

    // Log connection info if logger provided
    logger ?|>+ (fun log ->
      log.Info "Environment: %O" x
      log.Info "User: %s" usr
    )

    try
      match x.mfaAppId, x.mfaReturnUrl with
      | None,_
      | _,None
      | Some "",_
      | _,Some "" ->
        Connection.Connect(x.url, x.apToUse, usr, pwd, dmn)
      | Some appId, Some returnUrl ->
        Connection.Connect(x.url, usr, pwd, appId, returnUrl)
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


  /// Runs an executable with the given arguments and also passes on necessary login details for CRM if specified
  member x.executeProcess(exeLocation, ?args, ?urlParam, ?usrParam, ?pwdParam, ?apParam, ?dmnParam, ?paramToString) =
    let usr, pwd, dmn = x.getCreds()
    let givenArgs = args ?|> List.ofSeq ?| List.empty
    let envArgs = 
      [ urlParam ?|> fun k -> k, x.url.ToString()
        usrParam ?|> fun k -> k, usr
        pwdParam ?|> fun k -> k, pwd
        dmnParam ?|> fun k -> k, dmn
        apParam ?|> fun k -> k, x.apToUse.ToString()
      ] |> List.choose id
    
    let paramStringFunc = paramToString ?| Utility.toArg
    let argString = List.append envArgs givenArgs |> Utility.toArgString paramStringFunc

    let exeName = System.IO.Path.GetFileName exeLocation
    Utility.postProcess (Utility.executeProcess(exeLocation, argString)) log exeName


  
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
          
          