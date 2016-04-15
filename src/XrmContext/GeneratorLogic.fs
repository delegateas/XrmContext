namespace DG.XrmContext

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Client

open IntermediateRepresentation
open Utility

open InterpretEntityMetadata

open System.CodeDom
open System.CodeDom.Compiler
open XrmCodeDom

module internal GeneratorLogic =

  type RawState = {
    metadata: EntityMetadata[]
  }

  type InterpretedState = {
    outputDir: string
    entities: XrmEntity[]
    ns: string
    context: string option
  }


  (** Resource helpers *)
  let getResourceLines resName =
    let assembly = Assembly.GetExecutingAssembly()
    use res = assembly.GetManifestResourceStream(resName)
    use sr = new StreamReader(res)

    seq {
      while not sr.EndOfStream do yield sr.ReadLine ()
    } |> List.ofSeq

  // Check if line ends a versioned code block
  let versionCheckEndLine line =
    let m = Regex("^\s*//ENDVERSIONCHECK").Match(line)
    m.Success

  // Check if line indicates the start of a versioned code block or should be otherwise skipped
  let (|Supported|NotSupported|SkipLine|NormalLine|) (sdkVersion, line) =
    let m = Regex("^\s*//VERSIONCHECK\s+(\d)(?:\.(\d))?(?:\.(\d))?(?:\.(\d))?").Match(line)
    if not m.Success then 
      match versionCheckEndLine line with
      | true  -> SkipLine
      | false -> NormalLine
    else
      let getGroup (idx:int) = parseInt m.Groups.[idx].Value |? 0
      match checkVersion (getGroup 1, getGroup 2, getGroup 3, getGroup 4) sdkVersion with
      | true  -> Supported
      | false -> NotSupported

  // Remove unsupported lines based on the given version
  let removeUnsupportedLines sdkVersion (lines:string seq) =
    lines
    |> Seq.fold (fun (resLines,doRemove) line ->
      if doRemove then
        (resLines, versionCheckEndLine line |> not)
      else
        match sdkVersion, line with
        | Supported
        | SkipLine     -> (resLines, false)
        | NotSupported -> (resLines, true)
        | NormalLine   -> (line::resLines, false)
    ) ([], false)
    |> fst 
    |> Seq.rev


  (** Generation functionality *)

  /// Clear any previously output files
  let clearOldOutputFiles out =
    printf "Clearing old files..."
    let rec emptyDir path =
      Directory.EnumerateFiles(path, "*.d.ts") |> Seq.iter File.Delete
      let dirs = Directory.EnumerateDirectories(path, "*") 
      dirs |> Seq.iter (fun dir ->
        emptyDir dir
        try Directory.Delete dir
        with ex -> ())

    Directory.CreateDirectory out |> ignore
    emptyDir out
    printfn "Done!"


  // Proxy helper that makes it easy to get a new proxy instance
  let proxyHelper xrmAuth () =
    let ap = xrmAuth.ap |? AuthenticationProviderType.OnlineFederation
    let domain = xrmAuth.domain |? ""
    CrmAuth.authenticate
      xrmAuth.url ap xrmAuth.username 
      xrmAuth.password domain
    ||> CrmAuth.proxyInstance


  /// Connect to CRM with the given authentication
  let connectToCrm xrmAuth =
    printf "Connecting to CRM..."
    let proxy = proxyHelper xrmAuth ()
    printfn "Done!"
    proxy

  /// Retrieve version from CRM
  let retrieveCrmVersion mainProxy =
    printf "Retrieving CRM version..."

    let version = 
      CrmBaseHelper.retrieveVersion mainProxy

    printfn "Done!"
    version
    

  /// Retrieve all the necessary CRM data
  let retrieveCrmData entities mainProxy proxyGetter =
    printf "Fetching entity metadata from CRM..."

    let rawEntityMetadata = 
      match entities with
      | None -> CrmBaseHelper.getAllEntityMetadata mainProxy
      | Some logicalNames -> 
        CrmBaseHelper.getSpecificEntitiesAndDependentMetadata proxyGetter logicalNames

    printfn "Done!"
    { RawState.metadata = rawEntityMetadata }



  /// Interprets the raw CRM data into an intermediate state used for further generation
  let interpretCrmData out ns deprecatedPrefix context sdkVersion (rawState:RawState) =
    printf "Interpreting data..."
    let entityMap = 
      rawState.metadata
      |> Array.Parallel.map (fun em -> em.LogicalName, em)
      |> Map.ofArray

    let entityNames = 
      rawState.metadata
      |> Array.Parallel.map (fun em -> em.SchemaName)
      |> Set.ofArray

    let entityMetadata =
      rawState.metadata 
      |> Array.Parallel.map (interpretEntity entityNames entityMap deprecatedPrefix sdkVersion)
    printfn "Done!"

    { InterpretedState.entities = entityMetadata
      outputDir = out
      ns = ns
      context = context
    }


  /// Gets all the entities related to the given solutions and merges with the given entities
  let getFullEntityList entities solutions proxy =
    printf "Figuring out which entities should be included in the context.."
    let solutionEntities = 
      match solutions with
      | Some sols -> 
        sols 
        |> Array.map (CrmBaseHelper.retrieveSolutionEntities proxy)
        |> Seq.concat
        |> Set.ofSeq
      | None -> Set.empty

    let allEntities =
      match entities with
      | Some ents -> Set.union solutionEntities (Set.ofArray ents)
      | None -> solutionEntities

    printfn "Done!"
    match allEntities.Count with
    | 0 -> 
      printfn "Creating context for all entities"
      None
    | _ -> 
      let entities = allEntities |> Set.toArray 
      printfn "Creating context for the following entities: %s" (String.Join(",", entities))
      entities
      |> Some
      

  /// Generates the code represented in the CodeDom object
  let createCodeDom (state:InterpretedState) =
    printf "Generating code..."
    let cu = MakeCodeUnit state.entities state.ns state.context

    let provider = CodeDomProvider.CreateProvider("CSharp")
    let path = Path.Combine(state.outputDir, "XrmContext.cs")
    use writer = new StreamWriter(path)
    let options = CodeGeneratorOptions()

    provider.GenerateCodeFromCompileUnit(cu, writer, options)
    printfn "Done!"

  // Create resource files
  let createResourceFiles sdkVersion out =
    getResourceLines "XrmExtensions.cs"
    |> removeUnsupportedLines sdkVersion
    |> fun lines -> File.WriteAllLines(Path.Combine(out, "XrmExtensions.cs"), lines)