(*** hide ***)
namespace DG.Daxif.Modules

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif

/// Implements functions which allows you to manipulate solutions
module Solution = 
  (**
  Solution
  ==============

  Creates a new publisher with the unique name `name`, 
  the display name `display` and with the given publisher `prefix`.
  *)
  /// Creates a new publisher with the given parameters.
  val public createPublisher : wsdl:Uri
     -> name:string
     -> display:string
     -> prefix:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Creates a new solution with the unique name `name`
  and the display name `display`. The `pubPrefix` indicates who the
  publisher is.
  *)
  /// Creates a new solution with the given parameters.
  val public create : wsdl:Uri
     -> name:string
     -> display:string
     -> pubPrefix:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Deletes a solution with the unique `solution` name.
  *)
  /// Deletes a solution with the given parameters.
  val public delete : wsdl:Uri
     -> solution:string
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Enables (`enable` true) or disables (`disable` false) the plug-in steps related
  to the solution with the unique name `solution`.
  *)
  /// Enables or disables plug-in steps for a solution with the given parameters.
  val public pluginSteps : wsdl:Uri
     -> solution:string
     -> enable:bool
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Enables (`enable` true) or disables (`disable` false) the workflows related
  to the solution with the unique name `solution`.
  *)
  /// Enables or disables workflows for a solution with the given parameters.
  val public workflow : wsdl:Uri
     -> solution:string
     -> enable:bool
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Exports the given `solution` as a .zip file to the wanted 
  `location` on your local disk.
  *)
  /// Exports the solution as a zip file to the given location on disk.
  val public export : wsdl:Uri
     -> solution:string
     -> location:string
     -> managed:bool
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Imports the solution from the .zip file found at `location`.

  Performs publish if it is not a `managed` solution.
  *)
  /// Imports the solution from the .zip file found at location.
  val public import : wsdl:Uri
     -> solution:string
     -> location:string
     -> managed:bool
     -> ap:AuthenticationProviderType
     -> usr:string -> pwd:string -> domain:string -> logLevel:LogLevel -> unit
  (**
  Extracts the solution to the given `location`.

  TODO: More specific
  *)
  /// Extracts the solution to the given location.
  val public extract : solution:string
     -> location:string
     -> customizations:string
     -> map:string -> project:string -> logLevel:LogLevel -> unit
  (**
  Packs the solution to the given `location`.

  TODO: More specific
  *)
  /// Packs the solution to the given location.
  val public pack : solution:string
     -> location:string
     -> customizations:string
     -> map:string -> managed:bool -> logLevel:LogLevel -> unit
  (**
  Updates the service context files, including option sets, to the given 
  `location` with the CrmSvcUtil provided with the SDK.
  *)
  /// Updates the service context files at the specified location.
  val public updateServiceContext : wsdl:Uri
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string
     -> exeLocation:string -> lcid:int option -> logLevel:LogLevel -> unit
  (**
  Updates/creates the custom service context files, at the given 
  `location` with the XrmContext executable provided by Delegate A/S.
  *)
  /// Updates/creates the custom service context files, at the given 
  /// `location` with the XrmContext executable provided by Delegate A/S.
  val public updateCustomServiceContext : wsdl:Uri
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string
     -> exeLocation:string
     -> logLevel:LogLevel
     -> solutions:string list
     -> entities:string list -> extraArgs:(string * string) list -> unit
  (**
  Updates/creates TypeScript context files, at the given 
  `location` with the XrmDefinitelyTyped executable provided by Delegate A/S.
  *)
  /// Updates/creates TypeScript context files, at the given 
  /// `location` with the XrmDefinitelyTyped executable provided by Delegate A/S.
  val public updateTypeScriptContext : wsdl:Uri
     -> location:string
     -> ap:AuthenticationProviderType
     -> usr:string
     -> pwd:string
     -> domain:string
     -> exeLocation:string
     -> logLevel:LogLevel
     -> solutions:string list
     -> entities:string list -> extraArgs:(string * string) list -> unit
