namespace DG.XrmContext

open System
open Utility
open FileGeneration
open GenerationMain

open System.Diagnostics
open System.IO
open System.Runtime.Serialization.Json

type XrmContext private () =

  static member GenerateFromCrm(url, ?method, ?username, ?password, ?domain, ?ap, ?mfaAppId, ?mfaReturnUrl, ?mfaClientSecret, ?connectionString, ?out, ?entities, ?solutions, ?ns, ?context, ?deprecatedPrefix, ?sdkVersion, ?intersections,?labelMapping, ?oneFile) = 
    let xrmAuth = 
      { XrmAuthentication.url = Uri(url)
        method = method
        username = username
        password = password
        domain = domain
        ap = ap 
        clientId = mfaAppId
        returnUrl = mfaReturnUrl
        clientSecret = mfaClientSecret
        connectionString = connectionString}
    
    let rSettings = 
      { XcRetrievalSettings.entities = entities
        solutions = solutions
      }

    let gSettings = 
      { XcGenerationSettings.out = out
        sdkVersion = sdkVersion
        deprecatedPrefix = deprecatedPrefix
        ns = ns
        context = context
        intersections = intersections
        labelMapping = labelMapping
        oneFile = oneFile ?| true
       }
    
    XrmContext.GenerateFromCrm(xrmAuth, rSettings, gSettings)
  


  static member GenerateFromCrm(xrmAuth, rSettings, gSettings) =
    #if !DEBUG 
    try
    #endif 
      
      retrieveRawState xrmAuth rSettings
      |> generateFromRaw gSettings
      printfn "\nSuccessfully generated the C# context files."

    #if !DEBUG
    with ex -> getExceptionTrace ex |> failwithf "\nUnable to generate context files: %s"
    #endif



  static member SaveMetadataToFile(xrmAuth, rSettings, ?filePath) =
    #if !DEBUG 
    try
    #endif 
      
      let filePath = 
        filePath 
        ?>>? (String.IsNullOrWhiteSpace >> not)
        ?| "XcData.json"

      let serializer = DataContractJsonSerializer(typeof<RawState>)
      use stream = new FileStream(filePath, FileMode.Create)

      retrieveRawState xrmAuth rSettings
      |> fun state -> serializer.WriteObject(stream, state)
      printfn "\nSuccessfully saved retrieved data to file."

    #if !DEBUG
    with ex -> getExceptionTrace ex |> failwithf "\nUnable to generate context file: %s"
    #endif



  static member GenerateFromRawState(rawState, gSettings) =
    #if !DEBUG 
    try
    #endif 

      generateFromRaw gSettings rawState
      printfn "\nSuccessfully generated the C# context files."

    #if !DEBUG
    with ex -> getExceptionTrace ex |> failwithf "\nUnable to generate context files: %s"
    #endif


  static member GenerateFromFile(gSettings, ?filePath) =
    #if !DEBUG 
    try
    #endif 
      let filePath = 
        filePath 
        ?>>? (String.IsNullOrWhiteSpace >> not)
        ?| "XcData.json"

      let rawState =
        try
          let serializer = DataContractJsonSerializer(typeof<RawState>)
          use stream = new FileStream(filePath, FileMode.Open)
          serializer.ReadObject(stream) :?> RawState
        with ex -> failwithf "\nUnable to parse data file"

      generateFromRaw gSettings rawState
      printfn "\nSuccessfully generated the C# context files."

    #if !DEBUG
    with ex -> getExceptionTrace ex |> failwithf "\nUnable to generate context files: %s"
    #endif
