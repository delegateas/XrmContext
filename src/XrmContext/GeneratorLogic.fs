namespace DG.XrmContext

open System
open System.IO
open System.Reflection

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


  /// Connect to CRM with the given authentication
  let connectToCrm xrmAuth =
    let ap = xrmAuth.ap |? AuthenticationProviderType.OnlineFederation
    let domain = xrmAuth.domain |? ""

    printf "Connecting to CRM..."
    let manager,authToken =
      CrmAuth.authenticate 
        xrmAuth.url ap xrmAuth.username 
        xrmAuth.password domain
    let proxy = CrmAuth.proxyInstance manager authToken
    printfn "Done!"

    proxy

    

  /// Retrieve all the necessary CRM data
  let retrieveCrmData entities proxy =
    printf "Fetching entity metadata from CRM..."

    let rawEntityMetadata = 
      match entities with
      | None -> CrmDataHelper.getAllEntityMetadata proxy
      | Some logicalNames -> 
        let set = logicalNames |> Set.ofArray

        let mainEntities =
          logicalNames
          |> Array.map (CrmDataHelper.retrieveEntityAndDependentMetadata proxy set)
          |> List.concat
        
        let needActivityParty =
          not (set.Contains "activityparty") &&
          mainEntities 
          |> List.exists (fun m -> 
            m.Attributes 
            |> Array.exists (fun a -> 
              a.AttributeType.GetValueOrDefault() = AttributeTypeCode.PartyList))

        if needActivityParty then 
          (CrmDataHelper.retrieveActivityPartyAndDependentMetadata proxy set) @ mainEntities
        else mainEntities
        |> Array.ofList
        |> Array.distinctBy (fun m -> m.LogicalName)
    

    printfn "Done!"
    { RawState.metadata = rawEntityMetadata }



  /// Interprets the raw CRM data into an intermediate state used for further generation
  let interpretCrmData out ns context deprecatedPrefix (rawState:RawState) =
    printf "Interpreting data..."
    let entityMap = 
      rawState.metadata
      |> Array.Parallel.map (fun em -> em.LogicalName, em)
      |> Map.ofArray

    let entityMetadata =
      rawState.metadata 
      |> Array.map (interpretEntity entityMap deprecatedPrefix)
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
        |> Array.map (CrmDataHelper.retrieveSolutionEntities proxy)
        |> Seq.concat
        |> Set.ofSeq
      | None -> Set.empty

    let allEntities =
      match entities with
      | Some ents -> Set.union solutionEntities (Set.ofArray ents)
      | None -> solutionEntities

    match allEntities.Count with
    | 0 -> None
    | _ -> 
      let entities = allEntities |> Set.toArray 
      printfn "Done!"
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

  let createResourceFiles out =
    getResourceLines "XrmExtensions.cs"
    |> fun lines -> File.WriteAllLines(Path.Combine(out, "XrmExtensions.cs"), lines)