module internal DG.Daxif.Common.CrmAuth

open System.Net
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open DG.Daxif


// Get credentials based on provider, username, password and domain
let internal getCredentials provider username (password: string) domain =

  let ac = AuthenticationCredentials()

  match provider with
  | AuthenticationProviderType.ActiveDirectory ->
      ac.ClientCredentials.Windows.ClientCredential <-
        new NetworkCredential(username, password, domain)

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

  match serviceManagement.AuthenticationType with
  | AuthenticationProviderType.ActiveDirectory ->
      new OrganizationServiceProxy(serviceManagement, ac.ClientCredentials)
  | _ ->
      new OrganizationServiceProxy(serviceManagement, ac.SecurityTokenResponse)

// Authentication
let authenticate org ap username password domain =
  let m = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(org)
  let at = m.Authenticate(getCredentials ap username password domain)
  m,at

