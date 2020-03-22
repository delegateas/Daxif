module Delegate.Daxif.Test.EnvironmentMock

open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open Microsoft

let connectionMock customService = {
  new IConnection with
    member this.GetCrmServiceClient(): Xrm.Tooling.Connector.CrmServiceClient = 
      raise (System.NotImplementedException())
    member this.GetProxy(): OrganizationServiceProxy = 
      raise (System.NotImplementedException())
    member this.GetService() =
      customService
}

let CreateEnvironment service = {
  new IEnvironment with
    member this.ap = AuthenticationProviderType.None
    member this.clientId = Some "test"
    member this.clientSecret = Some "test"
    member this.creds = raise (System.NotImplementedException())
    member this.method = ConnectionType.Proxy
    member this.name = "Test Environment"
    member this.returnUrl = Some "https://www.daxif.com/test"
    member this.url = System.Uri("https://www.crm-test.dynamics.com/")
    member this.connect(_: ConsoleLogger option): IConnection =
     connectionMock service
    member this.executeProcess(_: string, _: 'a option, _: string option, _: string option, _: string option, _: string option, _: string option, _: (string * string -> string) option): unit = 
      raise (System.NotImplementedException())
    member this.getCreds() =
      raise (System.NotImplementedException())
    member this.logAuthentication(log: ConsoleLogger): unit = 
      log.Verbose "Name: %s" this.name
      log.Verbose "Url: %O" this.url
}
