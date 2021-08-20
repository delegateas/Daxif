module DG.Daxif.Modules.Solution.AttributeMapping

open System
open System.IO
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility
open DG.Daxif.Common.CrmDataHelper
open System
open Microsoft.Crm.Sdk
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Query
open DG.Daxif
open DG.Daxif.Common.Utility

open CrmUtility
open Microsoft.Crm.Sdk.Messages

let createAttributeMapping proxy (mappings: (string * string * string * string) []) =
    log.Info "Create attribute mappings"
    log.Info "Fetching attribute mappings"

    let someMappings, noneMappings =
        mappings
        |> Array.map
            (fun (sourceEntity, targetEntity, sourceAttr, targetAttr) ->
                let entity : Entity option =
                    CrmDataHelper.retrieveEntityMap proxy sourceEntity targetEntity

                match entity with
                | Some (entityMap) ->
                    (sourceEntity, targetEntity, sourceAttr, targetAttr),
                    Some(entityMap),
                    CrmDataHelper.retrieveAttributeMap proxy entityMap.Id sourceAttr targetAttr
                | None -> (sourceEntity, targetEntity, sourceAttr, targetAttr), None, None)
        |> fun x ->
            x
            |> Array.filter (fun (_, eMap, aMap) -> eMap.IsSome && aMap.IsSome),
            x
            |> Array.filter (fun (_, eMap, aMap) -> eMap.IsNone || aMap.IsNone)

    log.Info "Checking mapping length"

    if someMappings.Length > 0 then
        log.Info "Found %i existing attribute mapping(s)" (someMappings |> Array.length)

    if someMappings.Length = mappings.Length then
        log.Info "All mappings already exist"
    else

    if noneMappings.Length = 0 then
        log.Info "No mappings to create"

    else if noneMappings.Length > 0 then
        log.Info "Starting to create %i attribute mappings" (noneMappings |> Array.length)

        noneMappings
        |> Array.map
            (fun ((sourceEntity, targetEntity, sourceAttr, targetAttr), someEntityMap, someAttributeMap) ->
                match someEntityMap, someAttributeMap with
                | Some (eMap), None ->
                    log.Info
                        "Creating mapping between %s.%s -> %s.%s"
                        sourceEntity
                        sourceAttr
                        targetEntity
                        targetAttr

                    let attributeMap = new Entity("attributemap")
                    attributeMap.["entitymapid"] <- eMap.ToEntityReference()
                    attributeMap.["sourceattributename"] <- sourceAttr
                    attributeMap.["targetattributename"] <- targetAttr
                    Some(CrmDataHelper.makeCreateReq attributeMap :> OrganizationRequest)
                | _ -> None)
        |> Array.choose id
        |> CrmDataHelper.performAsBulk proxy
        |> Array.iter
            (fun resp ->
                if resp.Fault <> null then
                    log.Info "Unable to Create an attribute mapping : %s" resp.Fault.Message)
    log.Info "Done adding attribute mappings"

let removeAttributeMappings proxy (mappings: (string * string * string * string) []) =
    
    log.Info "Remove attribute mappings"

    log.Info "Fetching attribute mappings"

    let someMappings, noneMappings =
        mappings
        |> Array.map(fun (sourceEntity, targetEntity, sourceAttr, targetAttr) ->
                let entity : Entity option = CrmDataHelper.retrieveEntityMap proxy (sourceEntity) targetEntity

                match entity with
                | Some (entityMap) ->
                    (sourceEntity, targetEntity, sourceAttr, targetAttr), CrmDataHelper.retrieveAttributeMap proxy entityMap.Id sourceAttr targetAttr
                | None -> (sourceEntity, targetEntity, sourceAttr, targetAttr), None)
        |> fun x -> x |> Array.filter (fun (_, x) -> x.IsSome), x |> Array.filter (fun (_, x) -> x.IsNone)

    log.Info "Found %i existing attribute mapping(s)" (someMappings |> Array.length)

    if noneMappings.Length > 0 then
        log.Info "Unable to find following %i attribute mapping(s) to remove:" (noneMappings |> Array.length)

        noneMappings
        |> Array.iter
            (fun ((sourceEntity, targetEntity, sourceAttr, targetAttr), _) ->
                log.Info
                    "Attribute mapping %s.%s -> %s.%s does not exist"
                    sourceEntity
                    sourceAttr
                    targetEntity
                    targetAttr)

    if someMappings.Length > 0 then
        log.Info "Starting to delete found attribute mappings"

        someMappings
        |> Array.map
            (fun ((sourceEntity, targetEntity, sourceAttr, targetAttr), someEntity) ->
                match someEntity with
                | None -> None
                | Some (attrMap) ->
                    log.Info
                        "Deleting attribute mapping %s.%s -> %s.%s"
                        sourceEntity
                        sourceAttr
                        targetEntity
                        targetAttr

                    Some(CrmDataHelper.makeDeleteReq attrMap :> OrganizationRequest))
        |> Array.choose id
        |> CrmDataHelper.performAsBulk proxy
        |> Array.iter
            (fun resp ->
                if resp.Fault <> null then
                    log.Info "Unable to delete an attribute mapping : %s" resp.Fault.Message)

    log.Info "Done removing attribute mappings"
