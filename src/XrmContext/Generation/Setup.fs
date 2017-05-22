module internal DG.XrmContext.Setup

open System
open Microsoft.Xrm.Sdk.Metadata

open InterpretEntityMetadata
open IntermediateRepresentation
open Utility

/// Converts a XrmAttribute to a tuple which can be used as a comparable
let toComparableAttr (a: XrmAttribute) = 
  let ty = 
    match a.varType with
    | Default ty -> ty.FullName
    | OptionSet name -> name
    | PartyList -> "partylist"

  a.logicalName, a.schemaName, ty, a.canGet, a.canSet, a.isDeprecated

/// Intersects the attributes of the desired entities and returns the common attributes as XrmIntersects
let intersectEntities (entities: XrmEntity[]) (intersections: EntityIntersect[]) : XrmIntersect[] =  
  let entityMap =
    entities 
    |> Array.map (fun e -> e.logicalName, e.attr_vars)
    |> Map.ofArray

  intersections
  |> Array.map (fun (name, entities) -> 
    let commonAttributesByName =
      entities 
      |> Array.choose (fun e -> 
        match Map.tryFind e entityMap with
        | Some x -> x |> List.map toComparableAttr |> Some
        | None -> printfn "Could not find an entity with the logical name '%s'. It will not be included in the intersection." e; None
      )
      |> Array.map Set.ofList
      |> Set.intersectMany
      |> Set.map (fun (logicalName,_,_,_,_,_) -> logicalName)

    let commonAttributes = 
      entities 
      |> Array.tryPick (fun e -> Map.tryFind e entityMap)
      ?|> List.filter (fun a -> Set.contains a.logicalName commonAttributesByName)
      ?| List.empty

    name, commonAttributes
  )


/// Interprets the raw CRM data into an intermediate state used for further generation
let interpretCrmData (gSettings: XcGenerationSettings) out sdkVersion (rawState: RawState) =
  printf "Interpreting data..."

  let entityMap = 
    rawState.metadata
    |> Array.Parallel.map (fun em -> em.LogicalName, em)
    |> Map.ofArray

  let entityNames = 
    rawState.metadata
    |> Array.Parallel.map (fun em -> em.SchemaName)
    |> Set.ofArray

  let entityToIntersects =
    gSettings.intersections
    ?|> Array.fold (fun acc (name, entities) -> 
      entities 
      |> Array.fold (fun innerAcc e -> 
        let currentList = Map.tryFind e innerAcc ?| []
        Map.add e (name :: currentList) innerAcc
      ) acc
    ) Map.empty
    ?| Map.empty

  let entityMetadata =
    rawState.metadata 
    |> Array.Parallel.map (interpretEntity entityNames entityMap entityToIntersects gSettings.deprecatedPrefix sdkVersion)


  let intersections = 
    gSettings.intersections 
    ?|> intersectEntities entityMetadata 
    ?| [||]

  printfn "Done!"

  { InterpretedState.entities = entityMetadata
    outputDir = out 
    ns = gSettings.ns ?| null
    context = gSettings.context
    intersections = intersections
  }
      

