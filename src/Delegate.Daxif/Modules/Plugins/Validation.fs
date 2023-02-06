module internal DG.Daxif.Modules.Plugin.Validation

open System
open Microsoft.Xrm.Sdk
open DG.Daxif.Common
open CrmDataHelper

open CrmUtility
open Domain


/// Helper functions and monads for step based testing
type Result<'TValid,'TInvalid> = 
  | Valid of 'TValid
  | Invalid of 'TInvalid

let bind switchFunction = function
  | Valid s -> switchFunction s
  | Invalid f -> Invalid f
           
let findInvalid  plugins invalidPlugins msg =
  match invalidPlugins |> Seq.tryPick Some with
    | Some(name,_) -> Invalid (sprintf msg name)
    | None -> Valid plugins

(** Functions testing different aspects of the plugins **)


let preOperationNoPreImages plugins =
  let invalids =
    plugins
    |> Seq.filter(fun (_,pl) ->
      let i' = 
        pl.images
        |> Seq.filter(fun image -> image.imageType = int ImageType.PostImage)
      (pl.step.executionStage = int ExecutionStage.Pre || 
        pl.step.executionStage = int ExecutionStage.PreValidation) &&
          not (Seq.isEmpty i'))

  findInvalid plugins invalids "Plugin %s: Pre execution stages does not support pre-images"


let postOperationNoAsync plugins =
  let invalidPlugins =
    plugins
    |> Seq.filter(fun (_,pl) -> 
      pl.step.executionMode = int ExecutionMode.Asynchronous && 
        pl.step.executionStage <> int ExecutionStage.Post)

  findInvalid plugins invalidPlugins "Plugin %s: Post execution stages does not support asynchronous execution mode"

let associateDisasociateSteps plugins =
  plugins
  |> Seq.filter(fun (_,pl) -> 
    pl.step.eventOperation = "Associate" ||
    pl.step.eventOperation = "Disassociate")
        
let associateDisassociateNoFilters plugins =
  let invalidPlugins =
    plugins
    |> associateDisasociateSteps
    |> Seq.filter(fun (_,pl) ->
      pl.step.filteredAttributes <> null)
            
  findInvalid plugins invalidPlugins "Plugin %s can't have filtered attributes"

let associateDisassociateNoImages plugins =
  let invalidPlugins =
    plugins
    |> associateDisasociateSteps
    |> Seq.filter(fun (_,pl) ->
      not (Seq.isEmpty pl.images))
            
  findInvalid plugins invalidPlugins "Plugin %s can't have images"

let associateDisassociateAllEntity plugins =
  let invalidPlugins =
    plugins
    |> associateDisasociateSteps
    |> Seq.filter(fun (_,pl) ->
      pl.step.logicalName <> "")
            
  findInvalid plugins invalidPlugins "Plugin %s must target all entities"

let preEventsNoPreImages plugins =
  let invalidPlugins =
    plugins
    |> Seq.filter(fun (_,pl) ->
      let i' = 
        pl.images
        |> Seq.filter(fun image -> image.imageType = int ImageType.PreImage)
      pl.step.eventOperation = "Create" &&
      not (Seq.isEmpty i'))
            
  findInvalid plugins invalidPlugins "Plugin %s: Create events does not support pre-images"

let postEventsNoPostImages plugins =
  let invalidPlugins =
    plugins
    |> Seq.filter(fun (_,pl) ->
      let i' = 
        pl.images
        |> Seq.filter(fun image -> image.imageType = int ImageType.PostImage)
      pl.step.eventOperation = "Delete" &&
      not (Seq.isEmpty i'))
            
  findInvalid plugins invalidPlugins "Plugin %s: Post-events does not support post-images"

let validUserContext proxy plugins =
  let invalidPlugins =
    plugins
    |> Seq.filter(fun (_,pl) -> pl.step.userContext <> Guid.Empty)
    |> Seq.filter(fun (_,pl) ->
      not (CrmDataHelper.exists proxy "systemuser" pl.step.userContext)
    )

  findInvalid plugins invalidPlugins "Plugin %s: Defined user context is not in the system"
    
// Collection of all validation steps
let validateAssociateDisassosiate =
  associateDisassociateNoFilters
  >> bind associateDisassociateNoImages
  >> bind associateDisassociateAllEntity

let validate proxy =
  postOperationNoAsync
  >> bind preOperationNoPreImages
  >> bind validateAssociateDisassosiate
  >> bind preEventsNoPreImages
  >> bind postEventsNoPostImages
  >> bind (validUserContext proxy)

let validatePlugins proxy plugins =
  plugins
  |> Seq.map (fun pl -> pl.step.name, pl)
  |> validate proxy
