module internal DG.XrmContext.InterpretEntityMetadata

open System
open IntermediateRepresentation
open InterpretOptionSetMetadata
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata
open Utility

  
let typeConv = function   
  | AttributeTypeCode.Boolean   -> typeof<bool>
  | AttributeTypeCode.DateTime  -> typeof<DateTime>
    
  | AttributeTypeCode.Memo      
  | AttributeTypeCode.EntityName
  | AttributeTypeCode.String    -> typeof<string>

  | AttributeTypeCode.BigInt    -> typeof<int64>

  | AttributeTypeCode.Double    -> typeof<double>
  | AttributeTypeCode.Decimal   -> typeof<decimal>

  | AttributeTypeCode.Integer   -> typeof<int>
    
  | AttributeTypeCode.Lookup    
  | AttributeTypeCode.Customer
  | AttributeTypeCode.Owner     -> typeof<EntityReference>

  | AttributeTypeCode.Money     -> typeof<Money>
  | AttributeTypeCode.Uniqueidentifier -> typeof<Guid>
  | AttributeTypeCode.ManagedProperty -> typeof<BooleanManagedProperty>

  | _                           -> typeof<obj>

let IsWrongYomi (haystack : string) =
  not(haystack.StartsWith("Yomi")) && haystack.Contains("Yomi")

let (|LabelIsNullOrNullspace|) (label:LocalizedLabel) = 
  label = null || String.IsNullOrWhiteSpace label.Label

let getDescription displayName (descriptionLabel:LabelGroup) =
  let desc = descriptionLabel.UserLocalizedLabel.Label

  [ desc; displayName |> sprintf "Display Name: %s" ] 
  |> fun l -> if List.isEmpty l then None else Some l

let getRawAttributeFields (attr:RawAttribute) =
    match attr with
    | StringAttribute (sa,_) -> sa
    | IntAttribute (ia,_) -> ia
    | BigIntAttribute bia -> bia
    | BooleanAttribute ba -> ba
    | DateTimeAttribute da -> da
    | DecimalAttribute da -> da
    | DoubleAttribute da -> da
    | EnumAttribute (ea, _) -> ea
    | FileAttribute fa -> fa
    | ImageAttribute ia -> ia
    | LookupAttribute la -> la
    | MemoAttribute ma -> ma
    | MoneyAttribute ma -> ma
    | VirtualAttribute va -> 
      match va with
      | OptionSetCollectionAttribute ea -> ea

let interpretVirtualAttribute (a:VirtualAttribute) (options:XrmOptionSet option) hasOptions =
  match a, hasOptions with
  | OptionSetCollectionAttribute attr, true -> Some (OptionSetCollection options.Value.displayName)
  | _ -> None

let interpretNormalAttribute aType (options:XrmOptionSet option) hasOptions  =
  match aType, hasOptions with
  | AttributeTypeCode.State,    true
  | AttributeTypeCode.Status,   true
  | AttributeTypeCode.Picklist, true -> Some (OptionSet options.Value.displayName)
  | AttributeTypeCode.PartyList, _   -> Some PartyList
  | _ -> typeConv aType |> Default |> Some

let interpretAttribute deprecatedPrefix labelmapping localizations entityNames (e:RawEntity) (attr:RawAttribute) =
  let a = getRawAttributeFields attr

  let canSet = a.IsValidForCreate || a.IsValidForUpdate
  let canGet = a.IsValidForRead || canSet

  let aType = a.AttributeType

  if not canGet ||
    IsWrongYomi a.SchemaName ||
    a.AttributeOf <> null then None, None
  else

    let options, hasOptions =
      match attr with
      | EnumAttribute enum -> 
        let options = interpretOptionSet entityNames e enum labelmapping localizations
        options, options.IsSome && options.Value.options.Length > 0
      | _ -> None, false

    let vTypeOption = 
      match attr with
      | VirtualAttribute va -> interpretVirtualAttribute va options hasOptions
      | _ -> interpretNormalAttribute aType options hasOptions

    match vTypeOption with
    | None -> None, None
    | Some vType ->

      let displayName = a.DisplayName.UserLocalizedLabel.Label
      let desc = getDescription displayName a.Description

      let maxLength =
        match attr with
        | StringAttribute (_,sa) -> sa.MaxLength
        | _ -> None

      let minValue, maxValue =
        match attr with
        | IntAttribute (_,iam) -> iam.MinValue, iam.MaxValue
        | _ -> None, None

      let isDeprecated = 
        match displayName, deprecatedPrefix with
        | x, Some prefix -> x.StartsWith(prefix)
        | _ -> false

      options, Some {
      XrmAttribute.schemaName = a.SchemaName
      logicalName = a.LogicalName
      displayName = displayName |> Some
      varType = vType
      maxLength = maxLength
      minValue = minValue
      maxValue = maxValue
      canSet = canSet
      canGet = a.IsValidForRead || canSet
      description = desc
      isDeprecated = isDeprecated }


let interpretRelationship (entityMap:Map<string,RawEntity>) referencing (rel:RawOneToMany) =
  let rEntityName = if referencing then rel.ReferencedEntity else rel.ReferencingEntity
  
  match Map.tryFind rEntityName entityMap with
  | None -> None
    | Some rEntity ->
      let sameEntity = rel.ReferencedEntity = rel.ReferencingEntity
      let name =
          match sameEntity with
          | false -> rel.SchemaName
          | true  ->
            match referencing with
            | true  -> sprintf "Referencing%s" rel.SchemaName
            | false -> sprintf "Referenced%s" rel.SchemaName
    
      let xRel = 
        { XrmRelationship.varName = name
          schemaName = rel.SchemaName
          attributeName = 
            if referencing then rel.ReferencingAttribute 
            else rel.ReferencedAttribute
          referencing = referencing
          relatedEntity = rEntity.SchemaName
          useEntityRole = sameEntity
        }

      Some (rEntity.LogicalName, xRel)


let interpretM2MRelationship (entityMap:Map<string,RawEntity>) lname (rel:RawManyToMany) =
  let otherLogicalName, attributeName =
    match lname with
    | x when x.Equals(rel.Entity1LogicalName) -> rel.Entity2LogicalName, rel.Entity1IntersectAttribute
    | x when x.Equals(rel.Entity2LogicalName) -> rel.Entity1LogicalName, rel.Entity2IntersectAttribute
    | _ -> null, null
    
  match Map.tryFind otherLogicalName entityMap with
  | None -> None
  | Some rEntity ->
    let xRel = 
      { XrmRelationship.varName = rel.SchemaName 
        schemaName = rel.SchemaName
        attributeName = attributeName
        referencing = false
        relatedEntity = rEntity.SchemaName 
        useEntityRole = false
      }
    
    Some (rEntity.LogicalName, xRel)


let interpretKeyAttribute attrTypeMap (keyMetadata: RawKey) =
  let attrs = keyMetadata.Attributes |> Array.choose (fun x -> Map.tryFind x attrTypeMap)
  { XrmAlternateKey.displayName = keyMetadata.DisplayName.UserLocalizedLabel.Label
    XrmAlternateKey.schemaName = keyMetadata.SchemaName
    XrmAlternateKey.keyAttributes = attrs
  }

let interpretEntity entityNames (entityMap: Map<string,RawEntity>) entityToIntersects deprecatedPrefix labelmapping (localizations: int[] option) includeEntityTypeCode sdkVersion (metadata:RawEntity) =
  if (metadata.Attributes = null) then failwith "No attributes found!"

  // Attributes and option sets
  let opt_sets, attr_vars = 
    let filteredAttrs =
      if sdkVersion .>= (9,0,0,0)
      then metadata.Attributes
      else metadata.Attributes |> Array.filter(fun attr -> (getRawAttributeFields attr).AttributeType <> AttributeTypeCode.Virtual)
    
    filteredAttrs
    |> Array.map (interpretAttribute deprecatedPrefix labelmapping localizations entityNames metadata)
    |> Array.unzip

  let attr_vars = attr_vars |> Array.choose id |> Array.toList
    
  let opt_sets = 
    opt_sets 
    |> Seq.choose id 
    |> Seq.distinctBy (fun x -> x.displayName)
    |> Seq.toList

  // Status and state
  let stateAttr = opt_sets |> List.tryFind (fun x -> x.osType = XrmOptionSetType.State)
  let statusAttr = opt_sets |> List.tryFind (fun x -> x.osType = XrmOptionSetType.Status)

  // Alternate keys
  let alt_keys =
    match sdkVersion .>= (7,1,0,0) && not (isNull metadata.Keys) with
    | true ->
      let attrTypeMap = 
        attr_vars 
        |> List.map (fun a -> a.logicalName, (a. logicalName, a.schemaName, a.varType)) 
        |> Map.ofList

      metadata.Keys
      |> Array.map (interpretKeyAttribute attrTypeMap)
      |> List.ofArray
    | _ -> []

  // Relationships
  let handleOneToMany referencing = function
    | null -> Array.empty
    | x -> x |> Array.choose (interpretRelationship entityMap referencing)
    
  let handleManyToMany logicalName = function
    | null -> Array.empty
    | x -> x |> Array.choose (interpretM2MRelationship entityMap logicalName)

  let rel_entities, rel_vars = 
    [ metadata.OneToManyRelationships |> handleOneToMany false 
      metadata.ManyToOneRelationships |> handleOneToMany true 
      metadata.ManyToManyRelationships |> handleManyToMany metadata.LogicalName
    ] |> List.map Array.toList
      |> List.concat
      |> List.unzip

  let rel_entities = 
    rel_entities 
    |> Set.ofList |> Set.remove metadata.SchemaName |> Set.toList

  let desc = getDescription metadata.DisplayName.UserLocalizedLabel.Label metadata.Description

  let isIntersect = metadata.IsIntersect

  // Return the entity representation
  { typecode = if includeEntityTypeCode then metadata.ObjectTypeCode else None
    description = desc
    schemaName = metadata.SchemaName
    logicalName = metadata.LogicalName
    attr_vars = attr_vars
    rel_vars = rel_vars
    opt_sets = opt_sets
    alt_keys = alt_keys
    stateAttribute = stateAttr
    statusAttribute = statusAttr
    primaryNameAttribute = metadata.PrimaryNameAttribute
    primaryIdAttribute = metadata.PrimaryIdAttribute
    interfaces = Map.tryFind metadata.LogicalName entityToIntersects
    isIntersect = isIntersect
  }