module internal DG.Daxif.Common.InternalUtility

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Reflection
open System.Security.Cryptography
open System.Text
open System.Net.Sockets
open System.Net
open DG.Daxif


/// Global logger
let log = ConsoleLogger.Global

let assemblyVersion = 
  Assembly.GetExecutingAssembly().GetName().Version.ToString()
let assemblyFileVersion() = 
  FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion
let sha256CheckSum' bytes = 
  BitConverter.ToString(SHA256Managed.Create().ComputeHash(buffer = bytes))
              .Replace("-", String.Empty)
let sha256CheckSum s = Encoding.UTF8.GetBytes(s = s) |> sha256CheckSum'
let sha1CheckSum' bytes = 
  BitConverter.ToString(SHA1Managed.Create().ComputeHash(buffer = bytes))
              .Replace("-", String.Empty)
let sha1CheckSum s = Encoding.UTF8.GetBytes(s = s) |> sha1CheckSum'


let fnv1aHash' (bytes : byte []) = 
  let fnvp = (1 <<< 24) + (1 <<< 8) + 0x93 |> uint32
  let fnvob = 2166136261u
    
  let rec fnv1Hash'' h = 
    function 
    | i when i < (bytes.Length) -> 
      let h' = h ^^^ (bytes.[i] |> uint32) |> fun x -> x * fnvp
      fnv1Hash'' h' (i + 1)
    | _ -> h
  fnv1Hash'' fnvob 0
  
let fnv1aHash (key: string) = 
  match isNull key with
  | true -> ""
  | false ->
    key
    |> Encoding.UTF8.GetBytes
    |> fnv1aHash'
    |> fun x -> x.ToString("X")
  
let decode = Web.HttpUtility.HtmlDecode
let encode = Web.HttpUtility.HtmlEncode
let urldecode = Web.HttpUtility.UrlDecode
let urlencode s = Web.HttpUtility.UrlEncode(str = s)
let escape = Security.SecurityElement.Escape
let executingPath =
#if INTERACTIVE 
  Path.GetTempPath()
#else
  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
#endif
  
let ensureDirectory path = 
  match Directory.Exists(path) with
  | true -> ()
  | false -> Directory.CreateDirectory(path) |> ignore

let createTempFolder () =
  let folderName = Guid.NewGuid().ToString()
  let tmpFolder = Path.Combine(Path.GetTempPath(), folderName)
  tmpFolder |> ensureDirectory
  tmpFolder
  
let dMapLookup (dMap : Map<_, Map<_, _>>) key1 key2 = 
  match dMap.TryFind(key1) with
  | None -> None
  | Some map -> map.TryFind(key2)  

let touch path = 
  try 
    let as' = 
      (File.GetAttributes(path) ||| /// Ensure is set with OR
                                    FileAttributes.ReadOnly) 
      ^^^ /// And then remove with XOR
          FileAttributes.ReadOnly
    File.SetAttributes(path, as')
    File.SetLastWriteTimeUtc(path, DateTime.UtcNow)
  with ex -> failwith ex.Message
  
let fileToBase64 path = Convert.ToBase64String(File.ReadAllBytes(path))
  
let memoizeConcurrent f = 
  let dict = ConcurrentDictionary()
  fun x -> dict.GetOrAdd(Some x, lazy (f x)).Force()
  
let memoizeConcurrent' f = 
  let dict = ConcurrentDictionary()
  fun (x : 'a, y) -> dict.GetOrAdd(Some y, lazy (f (x, y))).Force()
  

let availablePort = 
  let l = new TcpListener(IPAddress.Loopback, 0)
  l.Start()
  let port = ((l.LocalEndpoint) :?> IPEndPoint).Port
  l.Stop()
  uint16 port
  

let daxifVersion =
  sprintf "DAXIF# v.%s" assemblyVersion

// Setup the description for anything synced with DAXIF
let syncDescription () = 
  sprintf "Synced with DAXIF# v.%s by '%s' at %s" 
    assemblyVersion
    Environment.UserName
    (DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz"))

let logAuthentication ap usr pwd domain (log: ConsoleLogger) =
  log.Verbose "Authentication Provider: %O" ap
  log.Verbose "User: %s" usr
  log.Verbose "Password: %s" pwd
  log.Verbose "Domain: %s" domain

let logVersion (log: ConsoleLogger) =
  log.Info "%s" daxifVersion