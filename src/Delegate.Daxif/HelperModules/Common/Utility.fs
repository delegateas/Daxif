namespace DG.Daxif.HelperModules.Common

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Reflection
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open DG.Daxif
open DG.Daxif.HelperModules.Common.ConsoleLogger


// TODO: Refactor to smaller names: timestamp -> ts
module internal Utility = 
  open System.Net.Sockets
  open System.Net
  
  /// Short version of `System.Collections.Generic.KeyValuePair<'a,'b>`
  type keyValuePair<'a, 'b> = System.Collections.Generic.KeyValuePair<'a, 'b>
  
  let (>>=) m f = Option.bind f m
  
  let (|=) a b = 
    match a with
    | Some v -> v
    | None -> b
  
  let unionToString (x : 'a) = 
    match FSharpValue.GetUnionFields(x, typeof<'a>, true) with
    | case, _ -> case.Name
  
  let stringToEnum<'T> str = Enum.Parse(typedefof<'T>, str) :?> 'T
  let timeStamp() = // ISO-8601
    DateTime.Now.ToString("o")
  let timeStamp'() = // Filename safe
    (timeStamp()).Replace(":", String.Empty)
  
  let timeStamp''() = // Only date
    let ts = timeStamp()
    ts.Substring(0, ts.IndexOf("T"))
  
  let cw (s : string) = Console.WriteLine(s)
  let cew (s : string) = Console.Error.WriteLine(s)
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
  // FNV-1a Hash: (non-cryptographic hash but really fast)
  // http://isthe.com/chongo/tech/comp/fnv/
  //
  // Test: ./fnv1a32 -s foo => 0xa9f37ed7
  let stb s = System.Text.Encoding.UTF8.GetBytes(s = s)
  
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
  
  let fnv1aHash key = 
    (key
     |> stb
     |> fnv1aHash').ToString("X")
  
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
    
  let readFile path = seq { yield! File.ReadLines(path) }
  
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
  
  let getFullException (ex : exn) = 
    let rec getFullException' (ex : exn) (level : int) = 
      ex.Message 
      + match ex.InnerException with
        | null -> String.Empty
        | ie -> 
          sprintf @" [Inner exception(%d): %s]" level 
            (getFullException' ie (level + 1))
    getFullException' ex 0
  
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
  
  let executeProcess' (exe, args, dir) = 
    let psi = new ProcessStartInfo(exe, args)
    psi.CreateNoWindow <- true
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.WorkingDirectory <- dir
    let p = Process.Start(psi)
    let o = new StringBuilder()
    let e = new StringBuilder()
    p.OutputDataReceived.Add(fun x -> o.AppendLine(x.Data) |> ignore)
    p.ErrorDataReceived.Add(fun x -> e.AppendLine(x.Data) |> ignore)
    p.BeginErrorReadLine()
    p.BeginOutputReadLine()
    p.WaitForExit()
    p.ExitCode, o.ToString(), e.ToString()
  
  let executeProcess (exe, args) = 
    let fn = System.IO.Path.GetFileName(exe)
    let dir = (System.IO.DirectoryInfo(exe)).FullName.Replace(fn, "")
    (exe, args, dir) |> executeProcess'
  
  let printProcessHelper ss log logl = 
    let ss : string = ss
    let log : ConsoleLogger = log
    let logl : LogLevel = logl
    ss.Split('\n')
    |> Array.filter (fun x -> not (String.IsNullOrWhiteSpace x))
    |> Array.iter (fun x -> log.WriteLine(logl, x))
  
  let printProcess proc (log : ConsoleLogger) = 
    function 
    | Some(0, os, _) -> printProcessHelper os log LogLevel.Verbose
    | Some(_, os, es) -> 
      printProcessHelper os log LogLevel.Verbose
      printProcessHelper es log LogLevel.Error
    | ex -> failwith (sprintf "%s threw an unexpected error: %A" proc ex)
  
  let availablePort = 
    let l = new TcpListener(IPAddress.Loopback, 0)
    l.Start()
    let port = ((l.LocalEndpoint) :?> IPEndPoint).Port
    l.Stop()
    uint16 port
  
  let (|ParseRegex|_|) regex str = 
    let m = Regex(regex).Match(str)
    match m.Success with
    | true -> 
      Some(List.tail [ for x in m.Groups -> x.Value ])
    | false -> None 

  let daxifVersion =
    sprintf "DAXIF# v.%s" assemblyVersion

  // Setup the description for anything synced with DAXIF
  let syncDescription () = 
    sprintf "Synced with DAXIF# v.%s by '%s' at %s" 
      assemblyVersion
      Environment.UserName
      (DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz"))

  let logAuthentication ap usr pwd domain (log: ConsoleLogger.ConsoleLogger) =
    log.WriteLine(LogLevel.Verbose, @"Authentication Provider: " + (ap.ToString()))
    log.WriteLine(LogLevel.Verbose, @"User: " + usr)
    log.WriteLine(LogLevel.Verbose, @"Password: " + pwd)
    log.WriteLine(LogLevel.Verbose, @"Domain: " + domain)