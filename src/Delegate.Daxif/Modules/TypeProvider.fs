namespace DG.Daxif.Modules.TypeProvider

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.Xrm.Sdk.Client
open ProviderImplementation.ProvidedTypes
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.TypeProviderHelper

[<TypeProvider>]
type XrmProvider(config : TypeProviderConfig) as this = 
  inherit TypeProviderForNamespaces()
  let ns = "DG.Daxif"
  let asm = Assembly.GetExecutingAssembly()
  
  let xrmType = 
    ProvidedTypeDefinition
      (asm, ns, "XrmProvider", Some typeof<obj>, HideObjectMethods = true)
  
  let parameters = 
    [ "uri", typeof<string>, None
      "usr", typeof<string>, None
      "pwd", typeof<string>, None
      
      "ap", typeof<AuthenticationProviderType>,
        Some(AuthenticationProviderType.OnlineFederation :> obj)
      "domain", typeof<string>, Some("" :> obj) ]
    |> List.map (fun (s, t, d) -> 
         match d with
         | Some o -> ProvidedStaticParameter(s, t, unbox o)
         | None -> ProvidedStaticParameter(s, t))

  do xrmType.DefineStaticParameters(parameters, fun typeName args ->
    let ty = 
      ProvidedTypeDefinition
        (asm, ns, typeName, Some typeof<obj>) //, HideObjectMethods = true)

    // Get parameters
    let uri = Uri(args.[0] :?> string)
    let usr = args.[1] :?> string
    let pwd = args.[2] :?> string
    let ap = args.[3] :?> AuthenticationProviderType
    let domain = args.[4] :?> string
    
    // Lazy authentication: only authenticate when actually needed, 
    // and only once in that runtime
    let proxy = 
      lazy
        let m = ServiceManager.createOrgService uri
        let tc = 
          m.Authenticate(Authentication.getCredentials ap usr pwd domain)
        ServiceProxy.getOrganizationServiceProxy m tc

    generateTypeProvider ty proxy)
  
  do this.AddNamespace(ns, [ xrmType ])

[<TypeProviderAssembly>]
()
