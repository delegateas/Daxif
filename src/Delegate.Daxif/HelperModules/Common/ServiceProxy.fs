namespace DG.Daxif.HelperModules.Common

open System
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Discovery

module internal ServiceProxy = 

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
  
  let getOrganizationServiceProxy (serviceManagement : IServiceManagement<IOrganizationService>) 
      (authCredentials : AuthenticationCredentials) = 
    let ac = authCredentials
    match serviceManagement.AuthenticationType with
      | AuthenticationProviderType.ActiveDirectory -> 
        new OrganizationServiceProxy(serviceManagement, ac.ClientCredentials)
      | _ -> 
        new OrganizationServiceProxy(serviceManagement, ac.SecurityTokenResponse)
    |> fun osp -> 
        osp.Timeout <- new TimeSpan(0, 59, 0)
        osp // Almost 1-hour timeout

  let proxyContext iServiceM authCred f =
    use p = getOrganizationServiceProxy iServiceM authCred
    f p
