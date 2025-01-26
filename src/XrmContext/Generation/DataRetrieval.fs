module internal DG.XrmContext.DataRetrieval

open System
open System.IO
open Utility

open CrmBaseHelper
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk

/// Connect to CRM with the given authentication
let connectToCrm xrmAuth =
  printf "Connecting to CRM..."
  let proxy = proxyHelper xrmAuth ()
  printfn "Done!"
  proxy

// Retrieve CRM entity name map
let retrieveEntityNameMap mainProxy =
  printf "Fetching entity names from CRM..."
    
  let metadata = 
    getAllEntityMetadataLight mainProxy

  let map =
    metadata
    |> Array.Parallel.map (fun m -> m.LogicalName, (m.SchemaName, m.EntitySetName))
    |> Map.ofArray

  printfn "Done!"
  map

// Retrieve CRM entity metadata
let retrieveEntityMetadata entities mainProxy =
  printf "Fetching specific entity metadata from CRM..."

  let rawEntityMetadata = 
    match entities with
    | None -> getAllEntityMetadata mainProxy
    | Some logicalNames -> 
      getSpecificEntitiesAndDependentMetadata mainProxy logicalNames

  printfn "Done!"
  rawEntityMetadata

/// Retrieve version from CRM
let retrieveCrmVersion mainProxy =
  printf "Retrieving CRM version..."

  let version = 
    CrmBaseHelper.retrieveVersion mainProxy

  printfn "Done!"
  version

let labelToDomain (label: Label) : LabelGroup =
  { 
    UserLocalizedLabel = {
      Label = label.UserLocalizedLabel.Label
      LanguageCode = label.UserLocalizedLabel.LanguageCode
    }    
    LocalizedLabels = 
      label.LocalizedLabels
      |> Array.ofSeq
      |> Array.map (fun l -> 
        { 
          Label = l.Label
          LanguageCode = l.LanguageCode
        })
  }

let getRawAttributeField (metadata: AttributeMetadata) : RawAttributeFields =
  {
    SchemaName = metadata.SchemaName
    LogicalName = metadata.LogicalName
    DisplayName = metadata.DisplayName |> labelToDomain
    Description = metadata.Description |> labelToDomain
    AttributeType = metadata.AttributeType ?> AttributeTypeCode.String
    IsValidForCreate = metadata.IsValidForCreate ?> true
    IsValidForRead = metadata.IsValidForRead ?> true
    IsValidForUpdate = metadata.IsValidForUpdate ?> true
    AttributeOf = metadata.AttributeOf
  }

let attributeToDomain (metadata: AttributeMetadata) : RawAttribute =
  match metadata with
  | :? StringAttributeMetadata as sam -> StringAttribute (getRawAttributeField metadata, { MaxLength = Option.ofNullable sam.MaxLength })
  | :? IntegerAttributeMetadata as iam -> IntAttribute (getRawAttributeField metadata, { MinValue = Option.ofNullable iam.MinValue; MaxValue = Option.ofNullable iam.MaxValue })

  | :? PicklistAttributeMetadata as pam -> EnumAttribute (getRawAttributeField metadata, { 
    OptionSetType = pam.OptionSet.OptionSetType ?> OptionSetType.Picklist
    IsGlobal = pam.OptionSet.IsGlobal ?> false
    SchemaName = pam.SchemaName
    Name = pam.LogicalName
    DisplayName = pam.DisplayName |> labelToDomain
    Options = 
      pam.OptionSet.Options
      |> Array.ofSeq
      |> Array.map (fun o -> { 
        Label = o.Label |> labelToDomain
        Description = o.Description |> labelToDomain
        Color = o.Color
        Value = o.Value ?> 0 })
        })
        

let keyToDomain (metadata: EntityKeyMetadata) : RawKey =
  { 
    DisplayName = metadata.DisplayName |> labelToDomain
    SchemaName = metadata.SchemaName
    Attributes = metadata.KeyAttributes
  }

let oneToManyToDomain (metadata: OneToManyRelationshipMetadata) : RawOneToMany =
  { 
    ReferencedEntity = metadata.ReferencedEntity
    ReferencingEntity = metadata.ReferencingEntity
    SchemaName = metadata.SchemaName
    ReferencedAttribute = metadata.ReferencedAttribute
    ReferencingAttribute = metadata.ReferencingAttribute
  }

let manyToManyDomain (metadata: ManyToManyRelationshipMetadata): RawManyToMany =
  { 
    SchemaName = metadata.SchemaName
    Entity1LogicalName = metadata.Entity1LogicalName
    Entity1IntersectAttribute = metadata.Entity1IntersectAttribute
    Entity2LogicalName = metadata.Entity2LogicalName
    Entity2IntersectAttribute = metadata.Entity2IntersectAttribute
  }

let metadataToDomain (metadata: EntityMetadata) : RawEntity =
  { 
    LogicalName = metadata.LogicalName
    SchemaName = metadata.SchemaName
    ObjectTypeCode = metadata.ObjectTypeCode
    DisplayName = metadata.DisplayName |> labelToDomain
    Description = metadata.Description |> labelToDomain
    IsPrivate = metadata.IsPrivate ?> false
    Attributes = metadata.Attributes |> Array.map attributeToDomain
    Keys = metadata.Keys |> Array.map keyToDomain
    OneToManyRelationships = metadata.OneToManyRelationships |> Array.map oneToManyToDomain
    ManyToOneRelationships = metadata.ManyToOneRelationships |> Array.map oneToManyToDomain
    ManyToManyRelationships = metadata.ManyToManyRelationships |> Array.map manyToManyDomain
    IsIntersect = metadata.IsIntersect ?> false
    PrimaryNameAttribute = metadata.PrimaryNameAttribute
    PrimaryIdAttribute = metadata.PrimaryIdAttribute
  }

/// Retrieve all the necessary CRM data
let retrieveCrmData entities mainProxy =

  let rawEntityMetadata = 
    retrieveEntityMetadata entities mainProxy

  { 
    RawState.metadata = rawEntityMetadata |> Array.map metadataToDomain
    crmVersion = retrieveCrmVersion mainProxy
  }

let retrieveCrmDataFromFiles (entities: string array option) (solutionPath: string) =
  let solutions = Directory.GetDirectories(solutionPath)
  let entityPaths = 
    solutions 
    |> Array.collect (fun p -> Directory.GetDirectories(Path.Combine(p, "entities")))

  let selectedEntityPaths =
    match entities with
    | Some ents ->
      let entitySet = Set.ofArray ents
      entityPaths
      |> Array.filter (fun p -> entitySet.Contains (Path.GetFileName(p)))
    | None -> entityPaths

  let solution

  { 
    RawState.metadata = solutionEntities
    crmVersion = (9, 0, 0, 0)
  }

/// Gets all the entities related to the given solutions and merges with the given entities
let getFullEntityList entities solutions proxy =
  printf "Figuring out which entities should be included in the context.."
  let solutionEntities = 
    match solutions with
    | Some sols -> 
      sols 
      |> Array.map (CrmBaseHelper.retrieveSolutionEntities proxy)
      |> Seq.concat |> Set.ofSeq
    | None -> Set.empty

  let finalEntities =
    match entities with
    | Some ents -> Set.union solutionEntities (Set.ofArray ents)
    | None -> solutionEntities

  printfn "Done!"
  match finalEntities.Count with
  | 0 -> 
    printfn "Creating context for all entities"
    None
  | _ -> 
    let entitySet = finalEntities |> Set.toArray 
    printfn "Creating context for the following entities: %s" (String.Join(",", entitySet))
    Some entitySet


let getFullEntityListFromFiles (solutionPaths: string array) (entities: string array option) (solutions: string array option) =
  printf "Figuring out which entities should be included in the context.."
  let solutionSet = 
    match solutions with
    | Some sol -> sol |> Set.ofArray
    | None -> Set.empty

  let solutionEntities =
    solutionPaths
    |> Array.filter(fun p -> solutionSet |> Set.contains (Path.GetFileName(p)))
    |> Array.collect (fun p -> Directory.GetDirectories(Path.Combine(p, "entities")))
    |> Array.map (fun p -> Path.GetFileName(p))
    |> Set.ofArray

  let finalEntities =
    match entities with
    | Some ents -> Set.union solutionEntities (Set.ofArray ents)
    | None -> solutionEntities

  printfn "Done!"
  match finalEntities.Count with
  | 0 -> 
    printfn "Creating context for all entities"
    None
  | _ -> 
    let entitySet = finalEntities |> Set.toArray 
    printfn "Creating context for the following entities: %s" (String.Join(",", entitySet))
    Some entitySet