module internal DG.XrmContext.InterpretOptionSetMetadata

open System.Text.RegularExpressions

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata

open Utility
open IntermediateRepresentation


let getLabelString (label:Label) (labelmapping:(string*string)[] option) =
  try
    label.UserLocalizedLabel.Label 
    |> Utility.applyLabelMappings labelmapping
    |> Utility.sanitizeString 
  with _ -> emptyLabel
  
let getUnsanitizedLabelString (label:Label) (labelmapping:(string*string)[] option) =
  try
    label.UserLocalizedLabel.Label 
    |> Utility.applyLabelMappings labelmapping
  with _ -> emptyLabel

let getMetadataString (metadata:OptionSetMetadataBase) labelMapping =
  getLabelString metadata.DisplayName labelMapping
  |> fun name -> 
    if name <> emptyLabel then name
    else metadata.Name

let getOptionSetType (optionSet:OptionSetMetadataBase) =
  let osType = optionSet.OptionSetType.GetValueOrDefault(OptionSetType.Picklist);
  match osType with
  | OptionSetType.State    -> XrmOptionSetType.State
  | OptionSetType.Status   -> XrmOptionSetType.Status
  | OptionSetType.Boolean  -> XrmOptionSetType.Boolean
  | _ -> XrmOptionSetType.Picklist

let getOptionsFromOptionSetMetadata (osm:OptionSetMetadata) labelMapping =
  if osm.Options.Count = 0 then None
  else

  let options =
    osm.Options
    |> Seq.indexed
    |> Seq.map (fun (idx, opt) ->
      let description =
        match getLabelString opt.Description labelMapping with
        | "_EmptyString" -> null
        | s -> s
      { label = getLabelString opt.Label labelMapping
        value = opt.Value.GetValueOrDefault()
        displayName = getUnsanitizedLabelString opt.Label labelMapping
        externalValue = opt.ExternalValue
        index = idx
        description = description
        color = opt.Color })
    
  options
  |> Seq.fold (fun (acc:Map<string,XrmOption list>) op ->
    if acc.ContainsKey op.label then
      acc.Add(
        op.label, 
        { op with label = sprintf "%s_%d" op.label (acc.[op.label].Length+1) } 
          :: acc.[op.label])
    else 
      acc.Add(op.label, [op])
  ) Map.empty
  |> Map.toArray |> Array.map snd |> List.concat 
  |> List.sortBy (fun op -> op.value) |> List.toArray |> Some


/// Interprets CRM OptionSetMetadata into intermediate type
let interpretOptionSet entityNames (entity:EntityMetadata option) (enumAttribute:EnumAttributeMetadata) (labelmappings:(string*string)[] option)=
  let optionSet = enumAttribute.OptionSet :> OptionSetMetadataBase
  if optionSet = null then None
  else

  let displayName = 
    match optionSet.OptionSetType.GetValueOrDefault(), 
          optionSet.IsGlobal.GetValueOrDefault(), 
          entity with
    | OptionSetType.State, _, Some x -> sprintf "%sState" x.SchemaName
    | _, false, Some x -> sprintf "%s_%s" x.SchemaName enumAttribute.SchemaName
    | _ -> optionSet.Name

  let displayName = 
    match entityNames |> Set.contains displayName with
    | true  -> sprintf "%s_Enum" displayName
    | false -> displayName

  match optionSet with
  | :? OptionSetMetadata as osm ->
    match getOptionsFromOptionSetMetadata osm labelmappings with
    | None -> None
    | Some options -> 
      { logicalName = optionSet.Name
        displayName = displayName
        osType = getOptionSetType optionSet
        options = options 
        isGlobal = optionSet.IsGlobal.Value} |> Some
      
  | :? BooleanOptionSetMetadata as bosm ->
    let options =
      [|  { label = getLabelString bosm.TrueOption.Label labelmappings
            value = 1
            displayName = getUnsanitizedLabelString bosm.DisplayName labelmappings
            externalValue = null
            index = 0
            description = getLabelString bosm.Description labelmappings
            color = null }
          { label = getLabelString bosm.FalseOption.Label labelmappings
            value = 0
            displayName = getUnsanitizedLabelString bosm.DisplayName labelmappings
            externalValue = null
            index = 1
            description = getLabelString bosm.Description labelmappings
            color = null } |]

    { logicalName = optionSet.Name
      displayName = displayName
      osType = XrmOptionSetType.Boolean
      options = options 
      isGlobal = optionSet.IsGlobal.Value} |> Some

  | _ -> None