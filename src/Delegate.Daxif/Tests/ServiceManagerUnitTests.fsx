//#load @"__temp__.fsx"
//#load @"AuthInfo.fsx"

open System
open System.Runtime.Serialization
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Query
open DG.Daxif.HelperModules.Common

let org = AuthInfo.wsdl//Uri(@"http://dg-crmdev-1.delegate.local:80/XrmFramework/XRMServices/2011/Organization.svc")
let ac = 
    Authentication.getCredentials // https://twitter.com/genTauro42/status/697358926870921217 :)
        AuthenticationProviderType.ActiveDirectory
        @"foo" @"bar" @"DELEGATE"

let m = ServiceManager.createOrgService org
let tc = m.Authenticate(ac)
let p = ServiceProxy.getOrganizationServiceProxy m tc

p.Retrieve("account", 
    new Guid(@"1C337E22-268A-E211-A627-00155D0C01D2"), 
    ColumnSet(true))
 .Attributes
//|> Seq.iter(fun a -> printfn "%A" a.Key)
|> Seq.iter(fun a -> printfn "%A" (a.Value))

//use p_ = ServiceProxy.getDiscoveryServiceProxy m ac //.getOrganizationServiceProxy m ac

//let unitTest =
//    [   
//        (* We call the function bar with 10. It should equal the number + 42 *)
//        (Data.bar 10 = (10 + 42));
//
//        (* We want the function to fail so we call it with another number not equal 
//           to the number + 42 *)
//        not (Data.bar 10 = (10 + 84));
//    ]
//
//let result = (unitTest |> List.reduce ( && ))
