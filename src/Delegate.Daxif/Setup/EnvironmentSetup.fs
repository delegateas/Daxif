namespace DG.Daxif

open System
open System.Collections.Generic
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open DG.Daxif.Setup
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

type Proxy = {serviceManager: IServiceManagement<IOrganizationService>; credentials: AuthenticationCredentials }
and CrmServiceClientOAuth = {orgUrl: Uri; username: string; password: string; clientId: string; returnUrl: string}
and CrmServiceClientClientSecret = {orgUrl: Uri; clientId: string; clientSecret: string}
and ConnectionMethod =
  | Proxy of Proxy
  | CrmServiceClientOAuth of CrmServiceClientOAuth
  | CrmServiceClientClientSecret of CrmServiceClientClientSecret
type ConnectionType = 
  | Proxy
  | OAuth
  | ClientSecret

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


type IConnection = 
  /// Connects to the environment and returns IOrganizationService
  abstract member GetService: unit -> IOrganizationService
  /// Connects to the environment and returns an OrganizationServiceProxy
  abstract member GetProxy: unit -> OrganizationServiceProxy
  /// Connects to the environment and returns a CrmServiceClient
  abstract member GetCrmServiceClient: unit -> Microsoft.Xrm.Tooling.Connector.CrmServiceClient

type IEnvironment =
  abstract member name: string
  abstract member url: Uri
  abstract member method: ConnectionType
  abstract member creds: Credentials option
  abstract member ap: AuthenticationProviderType
  abstract member clientId: string option
  abstract member returnUrl: string option
  abstract member clientSecret: string option
  /// Connects to the given environment
  abstract member connect: ?logger:ConsoleLogger -> IConnection
  /// Gets credentials for the given environment
  abstract member getCreds: unit -> string * string *string
  /// Prints authentication informations
  abstract member logAuthentication: log:ConsoleLogger -> unit
  /// Runs an executable with the given arguments and also passes on necessary login details for CRM if specified
  abstract member executeProcess: exeLocation:string 
    * ?args:#seq<string * string> 
    * ?urlParam:string 
    * ?usrParam:string 
    * ?pwdParam:string 
    * ?apParam:string 
    * ?dmnParam:string 
    * ?paramToString:(string * string -> string) -> unit

/// Used to get new proxy connections to a CRM environment
type Connection = {
  method: ConnectionMethod
} with
  /// Creates a connection with the given credentials
  static member Connect(env: Environment): IConnection =
    match env.method with
    | Proxy ->
      let usr, pwd, dmn = env.getCreds()
      let m, at = CrmAuth.authenticate env.url env.ap usr pwd dmn
      { method = ConnectionMethod.Proxy({serviceManager = m; credentials = at}) } :> IConnection
    | OAuth ->
      let usr, pwd, _ = env.getCreds()
      match env.clientId, env.returnUrl with
      | None,_
      | _,None -> let s = sprintf "Unable to connect using OAuth without client id and return url" in failwith s
      | Some appId, Some returnUrl ->
        { method = ConnectionMethod.CrmServiceClientOAuth({
          orgUrl = env.url
          username = usr
          password = pwd
          clientId = appId
          returnUrl = returnUrl
        })} :> IConnection
    | ClientSecret ->
      match env.clientId, env.clientSecret with
      | None,_
      | _,None -> let s = sprintf "Unable to connect using Client Secret without client id and client secret" in failwith s
      | Some appId, Some secret ->
        { method = ConnectionMethod.CrmServiceClientClientSecret({
          orgUrl = env.url
          clientId = appId
          clientSecret = secret
        })} :> IConnection

  /// Connects to the environment and returns an OrganizationServiceProxy
  member x.GetProxy() = 
    match x.method with
    | ConnectionMethod.Proxy proxy -> 
      CrmAuth.getOrganizationServiceProxy proxy.serviceManager proxy.credentials
    | ConnectionMethod.CrmServiceClientOAuth _
    | ConnectionMethod.CrmServiceClientClientSecret _ ->
      failwith "Not possible to get an OrganizationProxy usign OAuth or Client Secret. Get a CrmServiceClient instead"

  /// Connects to the environment and returns a CrmServiceClient
  member x.GetCrmServiceClient() =
    match x.method with
    | ConnectionMethod.Proxy _ -> 
      failwith "Unable to get CrmServiceClient with Proxy method"
    | ConnectionMethod.CrmServiceClientOAuth oauth ->
      CrmAuth.getCrmServiceClient oauth.username oauth.password oauth.orgUrl oauth.clientId oauth.returnUrl
    | ConnectionMethod.CrmServiceClientClientSecret clientSecret ->
      CrmAuth.getCrmServiceClientClientSecret clientSecret.orgUrl clientSecret.clientId clientSecret.clientSecret

  interface IConnection with 
    member x.GetService() =
      match x.method with
      | ConnectionMethod.Proxy _ -> 
        x.GetProxy() :> IOrganizationService
      | ConnectionMethod.CrmServiceClientOAuth _
      | ConnectionMethod.CrmServiceClientClientSecret _ -> 
        x.GetCrmServiceClient() :> IOrganizationService

    member x.GetProxy() = x.GetProxy()

    member x.GetCrmServiceClient() = x.GetCrmServiceClient()

/// Describes a connection to a Dynamics 365/CRM environment
and Environment = {
  name: string
  url: Uri
  method: ConnectionType
  creds: Credentials option
  ap: AuthenticationProviderType
  clientId: string option
  returnUrl: string option
  clientSecret: string option
} with 
  override x.ToString() = sprintf "%A (%A)" x.name x.url
 
  /// Gets the environment with the given name, if one exists
  static member Get(name) = EnvironmentHelper.get name

  /// Creates a new environment using the credentials and arguments given
  static member Create(name, url, ?method: ConnectionType, ?ap, ?creds, ?mfaAppId, ?mfaReturnUrl, ?mfaClientSecret, ?args) =
    let argMap = args ?|> parseArgs
    let credsToUse = 
      let usr = tryFindArgOpt ["username"; "usr"; "u"] argMap ?| ""
      let pwd = tryFindArgOpt ["password"; "pwd"; "p"] argMap ?| ""
      let dmn = tryFindArgOpt ["domain";   "dmn"; "d"] argMap ?| ""
      match (usr + pwd + dmn).Length > 0 with
      | true  -> Credentials.Create(usr, pwd, dmn) |> Some
      | false -> creds
    
    let argMethod =
      tryFindArgOpt ["method"] argMap ?>> 
      (fun method ->
        match method with
        | "OAuth" -> Some ConnectionType.OAuth
        | "ClientSecret" -> Some ConnectionType.ClientSecret
        | "Proxy" -> Some ConnectionType.Proxy
        | _ -> None)

    let env = {
      name = name
      url = Uri(url)
      method = argMethod ?|? method ?| ConnectionType.Proxy
      creds = credsToUse
      ap = ap ?| AuthenticationProviderType.OnlineFederation
      clientId = tryFindArgOpt ["mfaappid"] argMap ?|? mfaAppId
      returnUrl = tryFindArgOpt ["mfareturnurl"] argMap ?|? mfaReturnUrl
      clientSecret = tryFindArgOpt ["mfaclientsecret"] argMap ?|? mfaClientSecret 
    }

    EnvironmentHelper.add name env
    env

  /// Gets credentials for the given environment
  member x.getCreds() = 
    match x.creds with
    | None   -> CredentialManagement.getCredentialsFromKey x.name
    | Some c -> c.getValues()
    
  /// Connects to the given environment
  member x.connect(?logger: ConsoleLogger) =
    // Log connection info if logger provided
    logger ?|>+ (fun log ->
      log.Info "Environment: %O" x
    )

    try
      Connection.Connect(x)
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
        apParam ?|> fun k -> k, x.ap.ToString()
      ] |> List.choose id
    
    let paramStringFunc = paramToString ?| Utility.toArg
    let argString = List.append envArgs givenArgs |> Utility.toArgString paramStringFunc

    let exeName = System.IO.Path.GetFileName exeLocation
    Utility.postProcess (Utility.executeProcess(exeLocation, argString)) log exeName
    
  /// Prints authentication informations
  member x.logAuthentication (log: ConsoleLogger) =
      match x.method with 
      | Proxy -> 
        let usr,pwd,dmn = x.getCreds()
        log.Verbose "Authentication Provider: %O" x.ap
        log.Verbose "User: %s" usr
        log.Verbose "Password: %s" (String.replicate pwd.Length "*")
        log.Verbose "Domain: %s" dmn
      | OAuth ->
        let usr,pwd,dmn = x.getCreds()
        log.Verbose "Authentication Provider: %O" x.ap
        log.Verbose "User: %s" usr
        log.Verbose "Password: %s" (String.replicate pwd.Length "*")
        log.Verbose "Domain: %s" dmn
        log.Verbose "AppId: %O" x.clientId
        log.Verbose "ReturnUrl: %O" x.returnUrl
      | ClientSecret ->
        log.Verbose "AppId: %O" x.clientId 

  interface IEnvironment with
    member this.ap = this.ap
    member this.clientId = this.clientId
    member this.clientSecret = this.clientSecret
    member this.creds = this.creds
    member this.method = this.method
    member this.name = this.name
    member this.returnUrl = this.returnUrl
    member this.url = this.url
    member x.connect(?logger: ConsoleLogger): IConnection =
      x.connect(?logger = logger) 
    member x.executeProcess(exeLocation, ?args, ?urlParam, ?usrParam, ?pwdParam, ?apParam, ?dmnParam, ?paramToString): unit = 
      x.executeProcess(exeLocation, ?args = args, ?urlParam = urlParam, ?usrParam = usrParam, ?pwdParam = pwdParam, ?apParam = apParam, ?dmnParam = dmnParam, ?paramToString = paramToString)
    member x.getCreds() =
      x.getCreds()
    member x.logAuthentication(log: ConsoleLogger): unit = 
      x.logAuthentication(log)

    
  
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
          
          