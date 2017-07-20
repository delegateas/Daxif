namespace DG.Daxif.HelperModules.Common

open System.IO
open System.Text
open Portable.Licensing.Prime
open Portable.Licensing.Prime.Validation
open DG.Daxif
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility
open DG.Daxif.HelperModules.Common.ConsoleLogger

module internal License = 
  let private license = 
    let r = Utility.executingPath
    let l = r + @".\Delegate.Daxif.License.lic"
    try 
      let licenseChecks = 
        License.Load(File.ReadAllText(l, Encoding.UTF8)).Validate()
               .ExpirationDate().And()
               .Signature(LicenseSignature.publicKey).AssertValidLicense() 
        |> Seq.length
      let isValid = (licenseChecks = 0)
      isValid
    with ex -> false
  
  let licenseIsValidNoLog = license
  
  let licenseIsValid (log : ConsoleLogger) = 
    try 
      match license with
      | true -> log.WriteLine(LogLevel.Verbose, @"License is valid")
      | false -> 
        failwith 
          @"Invalid License. Please contact Delegate A/S at crm@delegate.dk"
    with ex -> failwith (getFullException ex)
