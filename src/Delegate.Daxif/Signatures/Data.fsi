(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

/// Implements data manipulation functions.
module Data = 
  (**
  Data
  ==========

  Tries to find entities of a certain type which matches
  the given `filter`.
  Returns the GUID of the first entity match.
  *)
  /// Checks if a certain entity exist which matches the given `filter`.
  val public exists : wsdl:Uri
     -> entityName:string
     -> filter:Map<string, obj>
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> Guid
  (**
  Count the entities of a certain type.
  *)
  /// Count the entities of a certain type.
  val public count : wsdl:Uri
     -> entityNames:string []
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Updates the state for each entity of a certain type 
  which matches the `filter` to that of `state`.
  *)
  /// Updates the state of the entities matching the given `filter`.
  val public updateState : wsdl:Uri
     -> entityName:string
     -> filter:Map<string, obj>
     -> state:int * int
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Reassign all records from a specific user, `userFrom`, to another user, `userTo`.
  *)
  /// Updates the state of the entities matching the given `filter`.
  val public reassignAllRecords : wsdl:Uri
     -> userFrom:Guid
     -> userTo:Guid
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Exports entities of a certain type to `location` on disk,
  serializing it into the wanted format (`serialize`).
  *)
  /// Exports entities of a certain type to the given `location`,
  /// serializing it into the wanted format.
  val public export : wsdl:Uri
     -> location:string
     -> entityNames:string []
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string 
     -> logLevel:LogLevel -> serialize:Serialize -> unit
  (**
  Exports entities of a certain type to `location` on disk which matches on the 
  `modifiedon` field where `date` is greater-equal, serializing it into the
  wanted format (`serialize`).
  *)
  /// Exports entities of a certain type to the given `location`,
  /// serializing it into the wanted format.
  val public exportDelta : wsdl:Uri
     -> location:string
     -> entityNames:string []
     -> date:DateTime
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string -> logLevel:LogLevel -> serialize:Serialize -> unit
  (**
  Exports entities of a certain type to `location` on disk which matches the 
  criterias specified on the given view/system view.
  *)
  /// Exports entities of a certain type to the given `location`,
  /// serializing it into the wanted format.
  val public exportView : wsdl:Uri
     -> location:string
     -> view:string
     -> user:bool
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string -> logLevel:LogLevel -> serialize:Serialize -> unit
  (**

  *)
  /// TODO:
  val public migrate : wsdl:Uri
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string
     -> logLevel:LogLevel
     -> serialize:Serialize -> keyValuePairs:Map<String, _> -> unit
  (**
  Imports entities from files at the given `location` with a matching
  extension to that of `serialize`.

  It then relates entity references to that of the target 
  as dictated by `data`, removes the primary attribute, 
  and adds the extra attributes given in `extraAttributes`.
  *)
  /// Imports entities from files at the given location.
  val public import : wsdl:Uri
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string
     -> logLevel:LogLevel
     -> serialize:Serialize
     -> extraAttributes:Map<String, _>
     -> data:Map<String, Map<Guid, Guid>> -> unit
  (**
        
  *)
  /// TODO:
  val public reassignOwner : wsdl:Uri
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string
     -> logLevel:LogLevel
     -> serialize:Serialize -> data:Map<String, Map<Guid, Guid>> -> unit
  (**
        
  *)
  /// TODO:
  val public associationImport : wsdl:Uri
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string
     -> logLevel:LogLevel
     -> seríalize:Serialize -> data:Map<String, Map<Guid, Guid>> -> unit