namespace DG.Daxif.Common

open System.Net
open Microsoft.Xrm.Sdk.Client
open Microsoft.Crm.Services.Utility

module internal Authentication = 
  let getCredentials provider username password domain = 
    let (password_ : string) = password
    let ac = AuthenticationCredentials()
    match provider with
    | AuthenticationProviderType.ActiveDirectory -> 
      ac.ClientCredentials.Windows.ClientCredential <- new NetworkCredential(username, 
                                                                             password_, 
                                                                             domain)
    | AuthenticationProviderType.LiveId -> // CRM Online using Live Id
      ac.ClientCredentials.UserName.UserName <- username
      ac.ClientCredentials.UserName.Password <- password_
      ac.SupportingCredentials <- new AuthenticationCredentials()
      ac.SupportingCredentials.ClientCredentials <- DeviceIdManager.LoadOrRegisterDevice
                                                      ()
    | AuthenticationProviderType.OnlineFederation -> // CRM Online using Office 365 
      ac.ClientCredentials.UserName.UserName <- username
      ac.ClientCredentials.UserName.Password <- password_
    | AuthenticationProviderType.Federation -> // Local Federation
      ac.ClientCredentials.UserName.UserName <- username
      ac.ClientCredentials.UserName.Password <- password_
    | _ -> failwith "No valid authentification provider was used."
    ac
