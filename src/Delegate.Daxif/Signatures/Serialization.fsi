(*** hide ***)
namespace DG.Daxif.Modules

open System
open System.Collections.Generic
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

/// Contains several functions to help the serialization process of data.
module Serialization =
    (**
    Serialization 
    ====================

    *)
    /// TODO:
    val public map2Dictionary :
        Map<'a,'b> -> 
        IDictionary<'a,'b> when 'a : comparison

    /// TODO:
    val public dictionary2Map :
        seq<KeyValuePair<'a,'b>> ->
        Map<'a,'b> when 'a : comparison

    /// TODO:
    val public serialize<'a> :
        Serialize ->
        'a ->
        byte array

    /// TODO:
    val public deserialize<'a> :
        SerializeType ->
        'a

    /// TODO:
    val public deserializeBinary<'a> : 
        byte [] ->
        'a

    /// TODO:
    val public serializeBinary: 
        'a -> 
        byte []

    /// TODO:
    val public xmlPrettyPrinterHelper: 
        byte [] ->
        byte []
