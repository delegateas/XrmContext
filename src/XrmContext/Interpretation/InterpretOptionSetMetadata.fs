module internal DG.XrmContext.InterpretOptionSetMetadata

open System.Text.RegularExpressions

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata

open Utility
open IntermediateRepresentation

let getDescription (opt : OptionMetadata, lcid: int) = 
   let desc = opt.Description.LocalizedLabels |> Seq.filter (fun f -> f.LanguageCode = lcid)
   match desc with
   | s when Seq.isEmpty s -> null
   | _ -> (desc |> Seq.head).Label


let getLocalized (opt: OptionMetadata) (labelmapping:(string*string)[] option) (localizations: int[] option) =    
  match localizations with 
  | None -> Seq.ofList [opt.Label.UserLocalizedLabel]
  | _ -> opt.Label.LocalizedLabels |> Seq.filter (fun f -> Array.contains f.LanguageCode localizations.Value)
  
  |> Seq.map(fun f -> f.LanguageCode, {displayName = f.Label |> Utility.applyLabelMappings labelmapping; description = getDescription(opt, f.LanguageCode)}) 
  |> Map

let getLabelString (label:Label) (labelmapping:(string*string)[] option) =
  let l = label.UserLocalizedLabel.Label 
  try
      l
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

let getOptionsFromOptionSetMetadata (osm:OptionSetMetadata) labelMapping localizations =
  if osm.Options.Count = 0 then None
  else

  let options =
    osm.Options
    |> Seq.indexed
    |> Seq.map (fun (idx, opt) ->
      //let description =
      //  match getLabelString opt.Description labelMapping with
      //  | "_EmptyString" -> null
      //  | s -> s
      { label = getLabelString opt.Label labelMapping
        value = opt.Value.GetValueOrDefault()        
        index = idx
        localization = getLocalized opt labelMapping localizations
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
let interpretOptionSet entityNames (entity:EntityMetadata option) (enumAttribute:EnumAttributeMetadata) (labelmappings:(string*string)[] option) (localizations: int[] option)=
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
    match getOptionsFromOptionSetMetadata osm labelmappings localizations with
    | None -> None
    | Some options -> 
      { logicalName = optionSet.Name
        displayName = displayName
        osType = getOptionSetType optionSet
        options = options 
        isGlobal = optionSet.IsGlobal.Value} |> Some

  | _ -> None