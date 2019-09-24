
open System

open Microsoft.Xrm.Sdk.Client

open DG.XrmContext
open Utility
open CommandLineHelper

let getXrmAuth parsedArgs = 
  let ap = 
    getArg parsedArgs "ap" (fun ap ->
      Enum.Parse(typeof<AuthenticationProviderType>, ap) 
        :?> AuthenticationProviderType)

  { XrmAuthentication.url = Uri(Map.find "url" parsedArgs)
    username = Map.find "username" parsedArgs
    password = Map.find "password" parsedArgs
    domain = Map.tryFind "domain" parsedArgs
    mfaAppId = Map.tryFind "mfaAppId" parsedArgs
    mfaReturnUrl = Map.tryFind "mfaReturnUrl" parsedArgs
    ap = ap 
  }

let getRetrieveSettings parsedArgs =
  let entities = getListArg parsedArgs "entities" (fun s -> s.ToLower())
  let solutions = getListArg parsedArgs "solutions" id

  { XcRetrievalSettings.entities = entities
    solutions = solutions
  }

let getGenerationSettings parsedArgs =
  
  let labelMapping = getListArg parsedArgs "labelMappings" (fun definition -> 
    let nameSplit = definition.IndexOf(":")
    if nameSplit < 0 then failwithf "Missing name specification in label Mapping list at: '%s'" definition
    
    let label = definition.Substring(0, nameSplit)
    let value = definition.Substring(nameSplit + 1)

    label, value)

  let intersections = getListArg parsedArgs "intersect" (fun definition -> 
    let nameSplit = definition.IndexOf(":")
    if nameSplit < 0 then failwithf "Missing name specification in intersect list at: '%s'" definition

    let name = definition.Substring(0, nameSplit) |> Utility.sanitizeString
    let list = definition.Substring(nameSplit + 1)

    let intersects = list.Split(';')

    name, intersects)

  let nsSanitizer ns =
    if String.IsNullOrWhiteSpace ns then String.Empty
    else ns.Split('.') |> Array.map sanitizeString |> String.concat "."

  { XcGenerationSettings.out = Map.tryFind "out" parsedArgs
    ns = getArg parsedArgs "namespace" nsSanitizer
    context = Map.tryFind "servicecontextname" parsedArgs
    deprecatedPrefix = Map.tryFind "deprecatedPrefix" parsedArgs
    sdkVersion = getArg parsedArgs "sdkVersion" parseVersion
    intersections = intersections
    labelMapping = labelMapping
    oneFile = getArg parsedArgs "oneFile" parseBoolish ?| true
  }

/// Load metadata from local file and generate
let loadGen parsedArgs =
  let filename = 
    match Map.tryFind "load" parsedArgs with
    | Some p -> p
    | None -> failwithf "No load argument found"

  XrmContext.GenerateFromFile(
    getGenerationSettings parsedArgs,
    filename)

/// Save metadata to file
let dataSave parsedArgs =
  let filename = 
    match Map.tryFind "save" parsedArgs with
    | Some p -> p
    | None -> failwithf "No load argument found"

  XrmContext.SaveMetadataToFile(
      getXrmAuth parsedArgs, 
      getRetrieveSettings parsedArgs,
      filename)

// Regular connect to CRM and generate
let connectGen parsedArgs =
  XrmContext.GenerateFromCrm(
    getXrmAuth parsedArgs, 
    getRetrieveSettings parsedArgs, 
    getGenerationSettings parsedArgs)


// Main executable function
let executeWithArgs argv =
  let parsedArgs = parseArgs argv Args.argMap

  match parsedArgs |> Map.tryPick (fun k v -> Args.flagArgMap.TryFind k) with
  | Some flagArg when flagArg = Args.genConfigFlag -> 
    Args.genConfig()

  | Some flagArg when flagArg = Args.loadFlag ->
    parsedArgs |> checkArgs Args.generationArgs |> loadGen

  | Some flagArg when flagArg = Args.saveFlag ->
    parsedArgs |> checkArgs Args.connectionArgs |> dataSave

  | _ -> 
    parsedArgs |> checkArgs Args.fullArgList |> connectGen


// Main method
[<EntryPoint>]
let main argv = 
  #if DEBUG
  executeWithArgs (List.ofArray argv)
  0
  #else
  try 
    showDescription()
    if argv.Length > 0 && Args.helpArgs.Contains argv.[0] then showUsage()
    else executeWithArgs (List.ofArray argv)
    0
  with ex ->
    eprintfn "%s" ex.Message
    1
  #endif