namespace DG.Daxif


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
  | SVG  = 11 // Added in 9.0, enum number unconfirmed

/// CRM Releases
/// Newer versions have higher values
type CrmReleases = 
  | CRM2011 = 2011
  | CRM2013 = 2013
  | CRM2015 = 2015
  | CRM2016 = 2016
  | D365    = 2017

/// State of asynchronous job
type AsyncJobState =
  | WaitingForResources = 0
  | Waiting             = 10
  | InProgress          = 20
  | Pausing             = 21
  | Canceling           = 22
  | Succeeded           = 30
  | Failed              = 31
  | Canceled            = 32

type Version = int * int * int * int
type VersionCriteria = Version option * Version option


type VersionIncrement =
  | Revision
  | Build
  | Minor
  | Major
