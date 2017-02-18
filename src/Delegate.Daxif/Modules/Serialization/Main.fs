module DG.Daxif.Modules.Serialization.Main

open System.Collections.Generic
open DG.Daxif
open DG.Daxif.Modules

let map2Dictionary (map : Map<_, _>) = 
  let map' = new Dictionary<_, _>()
  map |> Map.iter (fun k v -> map'.[k] <- v)
  map' :> IDictionary<_, _>
  
let dictionary2Map dictionary = 
  (dictionary :> seq<_>)
  |> Seq.map (|KeyValue|)
  |> Map.ofSeq
  
let serialize<'a> serialize obj = 
  SerializationHelper.serializeObjectToBytes<'a> serialize obj
  
let deserialize<'a> = 
  function 
  | SerializeType.BIN(bytes) -> 
    SerializationHelper.deserializeBinary'<'a> bytes
  | SerializeType.XML(xml) -> SerializationHelper.deserializeXML<'a> xml
  | SerializeType.JSON(json) -> SerializationHelper.deserializeJson<'a> json
  
let deserializeBinary<'a> bytes = 
  SerializationHelper.deserializeBinary'<'a> bytes
let serializeBinary (obj : 'a) = SerializationHelper.serializeBinary'<'a> obj
let xmlPrettyPrinterHelper bytes = 
  SerializationHelper.xmlPrettyPrinterHelper' bytes
