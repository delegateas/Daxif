namespace DG.Daxif.Common

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Discovery

module internal ServiceProxy = 

  type ClientManagement = 
    { IServiceM: Client.IServiceManagement<IOrganizationService>
      authCred: Client.AuthenticationCredentials }

  let getDiscoveryServiceProxy (serviceManagement : IServiceManagement<IDiscoveryService>) 
      (authCredentials : AuthenticationCredentials) = 
    let ac = authCredentials
    match serviceManagement.AuthenticationType with
      | AuthenticationProviderType.ActiveDirectory -> 
        new DiscoveryServiceProxy(serviceManagement, ac.ClientCredentials)
      | _ -> 
        new DiscoveryServiceProxy(serviceManagement, ac.SecurityTokenResponse) 
    |> fun dsp -> 
        dsp.Timeout <- new TimeSpan(0, 59, 0)
        dsp // Almost 1-hour timeout

    
