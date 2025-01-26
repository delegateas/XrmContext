module internal DG.XrmContext.InterpretOptionSetMetadata

open System.Text.RegularExpressions

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata

open Utility
open IntermediateRepresentation

let getDescription (opt : EnumOption, lcid: int) = 
   let desc = opt.Description.LocalizedLabels |> Seq.filter (fun f -> f.LanguageCode = lcid)
   match desc with
   | s when Seq.isEmpty s -> null
   | _ -> (desc |> Seq.head).Label


let getLocalized (opt: EnumOption) (labelmapping:(string*string)[] option) (localizations: int[] option) =    
  match localizations with 
  | None -> Seq.ofList [opt.Label.UserLocalizedLabel]
  | _ -> opt.Label.LocalizedLabels |> Seq.filter (fun f -> Array.contains f.LanguageCode localizations.Value)

  |> Seq.map(fun f -> f.LanguageCode, {displayName = f.Label |> Utility.applyLabelMappings labelmapping; description = getDescription(opt, f.LanguageCode)}) 
  |> Map


let getLabelString (label: LabelGroup) (labelmapping:(string*string)[] option) =
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

let getMetadataString (metadata: EnumAttributeFields) labelMapping =
  getLabelString metadata.DisplayName labelMapping
  |> fun name -> 
    if name <> emptyLabel then name
    else metadata.Name

let getOptionSetType (optionSet: EnumAttributeFields) =
  let osType = optionSet.OptionSetType;
  match osType with
  | OptionSetType.State    -> XrmOptionSetType.State
  | OptionSetType.Status   -> XrmOptionSetType.Status
  | OptionSetType.Boolean  -> XrmOptionSetType.Boolean
  | _ -> XrmOptionSetType.Picklist

let getOptionsFromOptionSetMetadata (osm:EnumAttributeFields) labelMapping localizations =
  if osm.Options.Length = 0 then None
  else
  
  let options =
    osm.Options
    |> Seq.indexed
    |> Seq.map (fun (idx, opt) ->
      { label = getLabelString opt.Label labelMapping
        value = opt.Value       
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
let interpretOptionSet entityNames (entity:RawEntity) ((attr,enum):EnumAttribute) (labelmappings:(string*string)[] option) (localizations: int[] option)=
  let displayName = 
    match enum.OptionSetType, 
          enum.IsGlobal, 
          entity with
    | OptionSetType.State, _, x -> sprintf "%sState" x.SchemaName
    | _, false, x -> sprintf "%s_%s" x.SchemaName enum.SchemaName
    | _ -> enum.Name

  let displayName = 
    match entityNames |> Set.contains displayName with
    | true  -> sprintf "%s_Enum" displayName
    | false -> displayName

  match getOptionsFromOptionSetMetadata enum labelmappings localizations with
  | None -> None
  | Some options -> 
    { logicalName = enum.Name
      displayName = displayName
      osType = getOptionSetType enum
      options = options 
      isGlobal = enum.IsGlobal} |> Some