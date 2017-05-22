module internal DG.XrmContext.InterpretOptionSetMetadata

open System.Text.RegularExpressions

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata

open Utility
open IntermediateRepresentation


let getLabelString (label:Label) =
  try
    label.UserLocalizedLabel.Label 
    |> Utility.sanitizeString
  with _ -> emptyLabel

let getMetadataString (metadata:OptionSetMetadataBase) =
  getLabelString metadata.DisplayName
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

let getOptionsFromOptionSetMetadata (osm:OptionSetMetadata) =
  if osm.Options.Count = 0 then None
  else

  let options =
    osm.Options
    |> Seq.map (fun opt ->
      { label = getLabelString opt.Label
        value = opt.Value.GetValueOrDefault() }) 
    
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
let interpretOptionSet entityNames (entity:EntityMetadata option) (enumAttribute:EnumAttributeMetadata) =
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
    match getOptionsFromOptionSetMetadata osm with
    | None -> None
    | Some options -> 
      { logicalName = optionSet.Name
        displayName = displayName
        osType = getOptionSetType optionSet
        options = options } |> Some
      
  | :? BooleanOptionSetMetadata as bosm ->
    let options =
      [|  { label = getLabelString bosm.TrueOption.Label
            value = 1 }
          { label = getLabelString bosm.FalseOption.Label
            value = 0 } |]

    { logicalName = optionSet.Name
      displayName = displayName
      osType = XrmOptionSetType.Boolean
      options = options } |> Some

  | _ -> None