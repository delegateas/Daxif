module internal DG.Daxif.Modules.Serialization.SerializationHelper

open System.IO
open System.Runtime.Serialization
open System.Runtime.Serialization.Formatters.Binary
open System.Runtime.Serialization.Json
open System.Text
open System.Xml.Linq
open DG.Daxif

let toBytes (x : string) = System.Text.Encoding.UTF8.GetBytes x
  
/// BIN
let serializeBinary'<'a> (obj : 'a) = 
  use stream = new MemoryStream()
  let bf = new BinaryFormatter()
  bf.Serialize(stream, obj)
  stream.ToArray()
  
let deserializeBinary'<'a> (bytes : byte []) = 
  use stream = new MemoryStream(bytes)
  let bf = new BinaryFormatter()
  bf.Deserialize(stream) :?> 'a
  
/// XML
let serializeXML<'a> (obj : 'a) = 
  use stream = new MemoryStream()
  let kt = seq { yield typeof<array<_>> }
  let dcs = new DataContractSerializer(typedefof<'a>, kt)
  dcs.WriteObject(stream, obj)
  stream.ToArray()
  
let deserializeXML<'a> (xml : string) = 
  use stream = new MemoryStream(toBytes xml)
  let kt = seq { yield typeof<array<_>> }
  let dcs = new DataContractSerializer(typedefof<'a>, kt)
  dcs.ReadObject(stream) :?> 'a
  
/// Json
let serializeJson<'a> (obj : 'a) = 
  use stream = new MemoryStream()
  let kt = seq { yield typeof<array<_>> }
  let dcjs = new DataContractJsonSerializer(typedefof<'a>, kt)
  dcjs.WriteObject(stream, obj)
  stream.ToArray()
  
let deserializeJson<'a> (json : string) = 
  use stream = new MemoryStream(toBytes json)
  let kt = seq { yield typeof<array<_>> }
  let dcs = new DataContractJsonSerializer(typedefof<'a>, kt)
  dcs.ReadObject(stream) :?> 'a
  
let xmlPrettyPrinterHelper' bytes = 
  let xml = XDocument.Parse(Encoding.UTF8.GetString(bytes))
  Encoding.UTF8.GetBytes(xml.ToString(SaveOptions.None))
  
let serializeObjectToBytes<'a> serialize obj = 
  serialize |> function 
  | Serialize.BIN -> serializeBinary'<'a> obj
  | Serialize.XML -> serializeXML<'a> obj |> xmlPrettyPrinterHelper'
  | Serialize.JSON -> serializeJson<'a> obj
  
let deserializeFileToObject<'a> serialize file = 
  serialize |> function 
  | Serialize.BIN -> 
    let bin = File.ReadAllBytes(file)
    deserializeBinary'<'a> bin
  | Serialize.XML -> 
    let xml = File.ReadAllText(file)
    deserializeXML<'a> xml
  | Serialize.JSON -> 
    let json = File.ReadAllText(file)
    deserializeJson<'a> json
