namespace DG.Daxif

// Info    = (1 <<< 0) ||| (1 <<< 2)
// Warning = (1 <<< 1) ||| (1 <<< 2)
// Error   = (1 <<< 2)
// Verbose = (1 <<< 0) ||| (1 <<< 1) ||| (1 <<< 2) ||| (1 <<< 3)
// File    = (1 <<< 0) ||| (1 <<< 1) ||| (1 <<< 2) ||| (1 <<< 3) ||| (1 <<< 4)
/// Defines the level of logging which should be done for specific message.
type LogLevel = // TODO: remove File and replace with guide of .exe > _out.txt 2> _error.log
  | None    =  0
  | Info    =  1 //  1 (2^0) Indicates logs for an informational message.
  | Warning =  3 //  2 (2^1) Indicates logs for a warning.
  | Error   =  7 //  4 (2^2) Indicates logs for an error.
  | Verbose = 15 //  8 (2^3) Indicates logs at all levels.
  | Debug   = 31 // 16 (2^4) Indicates logs for debugging
  | File    = 63 // 32 (2^5) Indicates logs at all levels and saved to a file

/// Represents what type of serialization should be done to encode/decode files
type Serialize = 
  | BIN
  | XML
  | JSON

//TODO:
type SerializeType = 
  | BIN of byte array
  | XML of string
  | JSON of string

/// Matches CRM2011/2013 OptionSet values
type WebResourceType = 
  | HTML =  1
  | HTM  =  1
  | CSS  =  2
  | JS   =  3
  | XML  =  4
  | XAML =  4
  | XSD  =  4
  | PNG  =  5
  | JPG  =  6
  | JPEG =  6
  | GIF  =  7
  | XAP  =  8
  | XSL  =  9
  | XSLT =  9
  | ICO  = 10

/// CRM Releases
type CrmReleases = 
  | CRM2011 = 2011
  | CRM2013 = 2013
  | CRM2015 = 2015
  | CRM2016 = 2016

/// State of asynchronous job
type AsyncJobState =
  | WaitingForResources = 0
  | Waiting             = 10
  | InProgress          = 20
  | Pausing             = 21
  | Canceling           = 22
  | Succeeded           = 30
  | Failed              = 31
  | Canceled             = 32


