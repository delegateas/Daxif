namespace DG.Daxif.Common

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Discovery

module internal ServiceManager = 
  let createDiscoveryService uri = 
    ServiceConfigurationFactory.CreateManagement<IDiscoveryService>(uri)
  let createOrgService uri = 
    ServiceConfigurationFactory.CreateManagement<IOrganizationService>(uri)
