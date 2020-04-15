module Delegate.Daxif.Test.TestBasic

open System
open Xunit
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Messages
open DG.Daxif
open OrganizationMock

type internal BasicOrg () = 
  inherit BaseMockOrg()
  [<DefaultValue>] val mutable publishCalls : int
  [<DefaultValue>] val mutable whoAmICalls : int
  interface IOrganizationService with
    override this.Execute( req: OrganizationRequest ) = 
      match req with
      | :? Messages.PublishAllXmlRequest -> 
        this.publishCalls <- this.publishCalls + 1
        Messages.PublishAllXmlResponse() :> OrganizationResponse
      | :? UpdateRequest -> 
        UpdateResponse() :> OrganizationResponse
      | :? Messages.WhoAmIRequest -> 
        this.whoAmICalls <- this.whoAmICalls + 1
        let resp = Messages.WhoAmIResponse()
        resp.Results.Add("UserId", Guid.NewGuid())
        Messages.WhoAmIResponse() :> OrganizationResponse
      | _ -> 
        raise (System.NotImplementedException())



[<Fact>]
let TestWhoAmI() =
  let mockOrg = BasicOrg()
  let mockEnv = EnvironmentMock.CreateEnvironment mockOrg
  let newService = mockEnv.connect().GetService()
  Assert.Equal(0, mockOrg.whoAmICalls)
  let req = Messages.WhoAmIRequest()
  let resp = newService.Execute(req) :?> Messages.WhoAmIResponse
  let id = resp.UserId
  Assert.Equal(1, mockOrg.whoAmICalls)
  Assert.NotNull id

[<Fact>]
let TestPublishAll() =
  let mockOrg = BasicOrg()
  let mockEnv = EnvironmentMock.CreateEnvironment mockOrg

  Assert.Equal(0, mockOrg.publishCalls)
  Solution.PublishCustomization(mockEnv)
  Assert.Equal(1, mockOrg.publishCalls)


