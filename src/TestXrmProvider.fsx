
#r @"Delegate.Daxif\bin\Debug\Microsoft.Xrm.Sdk.dll"
#r @"Delegate.Daxif\bin\Debug\Delegate.Daxif.dll"

open DG.Daxif
open Microsoft.Xrm.Sdk.Client

//[<Literal>]
//let usr = @"admin@tangedemo3.onmicrosoft.com"
//
//[<Literal>]
//let pwd = @"pass@word1"
//
//[<Literal>]
//let wsdl = @"https://tangedemo3.crm4.dynamics.com/XRMServices/2011/Organization.svc"
//
//[<Literal>]
//let ap = AuthenticationProviderType.OnlineFederation
let rootFolder = __SOURCE_DIRECTORY__
let unmanaged = @"\unmanaged\"
let zipSource = rootFolder + unmanaged + @"Foo" + @".zip"
let zipTarget = rootFolder + unmanaged + @"Foo_" + @".zip"

//  Diff.solution zipSource zipTarget LogLevel.Verbose


DG.Daxif.Modules.Diff.solution zipSource zipTarget LogLevel.Debug

