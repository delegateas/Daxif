namespace DG.Daxif

#nowarn "40"

open System
open System.IO
open System.Reflection
open System.Threading



/// Defines the level of logging which should be done for specific message.
type LogLevel = 
  /// Indicates logs for an error.
  | Error   =  1
  /// Indicates logs for a warning.
  | Warning =  3
  /// Indicates logs for an informational message.
  | Info    =  7
  /// Indicates logs at all levels.
  | Verbose = 15 
  /// Indicates logs for debugging
  | Debug   = 31 


type ConsoleLogger(logLevel : LogLevel) = 
  
  let mutable maxLevel = logLevel
  let threadSafe = ref String.Empty

  let ts = 
    DateTime.Now.ToString("o")
    |> fun str -> str.Substring(0, str.IndexOf("T"))
  let file' = 
    #if INTERACTIVE
      Path.GetTempPath() +
    #else
      Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
    #endif
      Path.DirectorySeparatorChar.ToString() + ts + "_DAXIF#.log"
  let ofc = Console.ForegroundColor
  let obc = Console.BackgroundColor
    
  let prettyPrint fc bc logLevel str = 
    Monitor.Enter threadSafe
    try 
      let ts = DateTime.Now.ToString("u")
      let prefix = sprintf "(%s) [%A]: " ts logLevel
      Console.ForegroundColor <- fc
      Console.BackgroundColor <- bc
      match logLevel = LogLevel.Warning || logLevel = LogLevel.Error with
      | true  -> eprintfn "%s%s" prefix str
      | false -> printfn "%s%s" prefix str
    finally
      Console.ForegroundColor <- ofc
      Console.BackgroundColor <- obc
      Monitor.Exit threadSafe
    
  let log logLevel str =
    match maxLevel.HasFlag logLevel with
    | false -> ()
    | true ->
      match logLevel with
      | LogLevel.Info    -> ConsoleColor.White, obc
      | LogLevel.Warning -> ConsoleColor.Yellow, obc
      | LogLevel.Error   -> ConsoleColor.White, ConsoleColor.Red
      | LogLevel.Verbose -> ConsoleColor.Gray, obc
      | LogLevel.Debug   -> ConsoleColor.DarkYellow, obc
      | _ -> failwithf "Invalid LogLevel"
      |> fun (fgc, bgc) -> prettyPrint fgc bgc logLevel str
  
  let formatLog logLevel format =
    Printf.ksprintf (log logLevel) format

  static member Global = ConsoleLogger(LogLevel.Verbose)
  member t.setLevelOption newLevel =
    match newLevel with
    | Some l -> maxLevel <- l
    | None   -> ()

  member t.WriteLine(logLevel, str) = 
    match maxLevel.HasFlag logLevel with
    | false -> ()
    | true -> log logLevel str

  member t.overwriteLastLine() = 
    Console.SetCursorPosition(0, Console.CursorTop-1)

  member t.Error format = formatLog LogLevel.Error format
  member t.Warn format = formatLog LogLevel.Warning format
  member t.Info format = formatLog LogLevel.Info format
  member t.Verbose format = formatLog LogLevel.Verbose format
  member t.Debug format = formatLog LogLevel.Debug format

