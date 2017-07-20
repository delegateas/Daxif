/// Contains the credential management logic of Daxif.
/// The first time you use a certain credential key, it prompts the user for input, and validates it. 
/// If valid, the login information is encrypted and stored locally in a "<key>.daxif" file in the Daxif folder.
/// Next time that credential key is used, it will load the login information from that file instead of prompting the user.
module internal DG.Daxif.Setup.CredentialManagement

open System
open System.IO
open System.Text
open System.Security.Cryptography
open DG.Daxif.Common.Utility


module internal HelperMethods =
  let protect (usr, pwd, dmn) =
    ProtectedData.Protect(Encoding.UTF8.GetBytes(sprintf "%s\n%s\n%s" usr pwd dmn), null, DataProtectionScope.CurrentUser)
    |> Convert.ToBase64String

  let unprotect toDecode =
    ProtectedData.Unprotect(Convert.FromBase64String(toDecode), null, DataProtectionScope.CurrentUser)
    |> Encoding.UTF8.GetString
    |> fun str -> 
      let split = str.Split('\n')
      split.[0], split.[1], split.[2]

  let getCredsFilePathFromKey key =
    Path.Combine(Directory.GetCurrentDirectory(), sprintf "%s.daxif" key)

  let loadCredsFromFile key =
    let pathToFile = getCredsFilePathFromKey key
    match File.Exists pathToFile with
    | true  -> Some (File.ReadAllText pathToFile |> unprotect)
    | false -> None


  let saveCredsToFile (key: string) creds =
    let pathToFile = getCredsFilePathFromKey key
    File.WriteAllText(pathToFile, protect creds)

  let getCredsFromInput () =
    printfn "Username: "
    let usrInput = Console.ReadLine()

    let split = usrInput.Split('\\')
    let dmn, usr =
      match split.Length > 1 with
      | true  -> 
        split |> Array.take (split.Length-1) |> String.concat "\\", 
        split.[split.Length-1]
      | false -> "", split.[split.Length-1]

    printfn "Password: "
    let pwd = Console.ReadLine()
    usr, pwd, dmn


  let getCredsFromInputAndStore key =
    let creds = getCredsFromInput()
    saveCredsToFile key creds
    creds


let getCredentials key =
  match HelperMethods.loadCredsFromFile key with
  | Some creds -> creds
  | None -> HelperMethods.getCredsFromInputAndStore key
  

let promptNewCreds key =
  let path = HelperMethods.getCredsFilePathFromKey key
  match File.Exists path with
  | false -> None
  | true  ->
    printfn "Do you want to delete the currently stored credentials and re-enter new ones? [y/n]"
    let response = Console.ReadLine()
    match response with
    | ParseRegex "(y|yes)" x ->
      File.Delete path
      Some (getCredentials key)
    | _ ->
      None
    
  