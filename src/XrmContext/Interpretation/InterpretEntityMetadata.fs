namespace DG.XrmContext

open System
open IntermediateRepresentation
open InterpretOptionSetMetadata
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata

module internal InterpretEntityMetadata =
  
  
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

  let (|IsWrongYomi|) (haystack : string) =
    not(haystack.StartsWith("Yomi")) && haystack.Contains("Yomi")



  let interpretAttribute deprecatedPrefix (e:EntityMetadata) (a:AttributeMetadata) =
    let canSet = a.IsValidForCreate.GetValueOrDefault() || a.IsValidForUpdate.GetValueOrDefault()
    let canGet = a.IsValidForRead.GetValueOrDefault() || canSet

    let aType = a.AttributeType.GetValueOrDefault()

    match canGet, aType, a.SchemaName with
      | false, _, _
      | _, AttributeTypeCode.Virtual, _
      | _, _, IsWrongYomi true           -> None, None
      | _ -> 
      
      let options, hasOptions =
        match a with
        | :? EnumAttributeMetadata as eam -> 
          let options = interpretOptionSet (Some e) eam
          options, options.IsSome && options.Value.options.Length > 0
        | _ -> None, false

      let vType = 
        match aType, hasOptions with
        | AttributeTypeCode.State,    true
        | AttributeTypeCode.Status,   true
        | AttributeTypeCode.Picklist, true -> OptionSet options.Value.displayName
        | AttributeTypeCode.PartyList, _   -> PartyList
        | _ -> typeConv aType |> Default
      

      let displayName = 
        match a.DisplayName.UserLocalizedLabel <> null with
        | true -> Some a.DisplayName.UserLocalizedLabel.Label
        | _ -> None

      let isDeprecated = 
        match displayName, deprecatedPrefix with
        | Some x, Some prefix -> x.StartsWith(prefix)
        | _ -> false

      let desc =
        match displayName, a.Description.UserLocalizedLabel with
        | None, null -> None
        | None, desc -> if String.IsNullOrWhiteSpace(desc.Label) then None else Some [desc.Label]
        | Some name, null -> Some [sprintf "Display Name: %s" name]
        | Some name, desc -> Some [desc.Label; sprintf "Display Name: %s" name]


      options, Some {
        XrmAttribute.schemaName = a.SchemaName
        logicalName = a.LogicalName
        varType = vType
        canSet = canSet
        canGet = a.IsValidForRead.GetValueOrDefault() || canSet
        description = desc
        isDeprecated = isDeprecated }


  let interpretRelationship (entityMap:Map<string,EntityMetadata>) referencing (rel:OneToManyRelationshipMetadata) =
    let rEntity =
        if referencing then rel.ReferencedEntity
        else rel.ReferencingEntity
        |> fun s ->
          match Map.tryFind s entityMap with
          | Some x -> x
          | None -> null
    
    // Do not create relationship if the related entity has not been created
    if rEntity = null then None
    else 

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


  let interpretM2MRelationship (entityMap:Map<string,EntityMetadata>) lname (rel:ManyToManyRelationshipMetadata) =
    let otherLogicalName, attributeName =
      match lname with
      | x when x.Equals(rel.Entity1LogicalName) -> rel.Entity2LogicalName, rel.Entity1IntersectAttribute
      | x when x.Equals(rel.Entity2LogicalName) -> rel.Entity1LogicalName, rel.Entity2IntersectAttribute
      | _ -> null, null
    
    let rEntity =
      match Map.tryFind otherLogicalName entityMap with
      | Some x -> x
      | None -> null

    // Do not create relationship if the related entity has not been created
    if rEntity = null then None
    else

    let xRel = 
      { XrmRelationship.varName = rel.SchemaName 
        schemaName = rel.SchemaName
        attributeName = attributeName
        referencing = false
        relatedEntity = rEntity.SchemaName 
        useEntityRole = false
      }
    
    Some (rEntity.LogicalName, xRel)



  let interpretEntity entityMap deprecatedPrefix (metadata:EntityMetadata) =
    if (metadata.Attributes = null) then failwith "No attributes found!"

    // Attributes and option sets
    let opt_sets, attr_vars = 
      metadata.Attributes 
      |> Array.map (interpretAttribute deprecatedPrefix metadata)
      |> Array.unzip

    let attr_vars = attr_vars |> Array.choose id |> Array.toList
    
    let opt_sets = 
      opt_sets |> Seq.choose id |> Seq.distinctBy (fun x -> x.displayName) 
      |> Seq.toList

    let stateAttr = opt_sets |> List.tryFind (fun x -> x.osType = XrmOptionSetType.State)
    let statusAttr = opt_sets |> List.tryFind (fun x -> x.osType = XrmOptionSetType.Status)
    
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


    // Return the entity representation
    { XrmEntity.typecode = metadata.ObjectTypeCode.GetValueOrDefault()
      schemaName = metadata.SchemaName
      logicalName = metadata.LogicalName
      attr_vars = attr_vars
      rel_vars = rel_vars
      opt_sets = opt_sets
      stateAttribute = stateAttr
      statusAttribute = statusAttr
      primaryNameAttribute = metadata.PrimaryNameAttribute
      primaryIdAttribute = metadata.PrimaryIdAttribute }