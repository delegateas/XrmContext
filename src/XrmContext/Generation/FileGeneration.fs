﻿module internal DG.XrmContext.FileGeneration

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions

open IntermediateRepresentation
open Utility

open System.CodeDom.Compiler
open XrmCodeDom

(** Resource helpers *)
let getResourceLines resName =
  let assembly = Assembly.GetExecutingAssembly()
  use res = assembly.GetManifestResourceStream("XrmContext.Resources." + resName)
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
    let getGroup (idx:int) = parseInt m.Groups.[idx].Value ?| 0
    match sdkVersion .>= (getGroup 1, getGroup 2, getGroup 3, getGroup 4) with
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
      | NormalLine   -> (line :: resLines, false)
  ) ([], false)
  |> fst
  |> Seq.rev

// Generates the code represented in the CodeDom object
let generateFromCu (outputDir: string) (className: string) (cu: CodeDom.CodeCompileUnit) =
  let provider = CodeDomProvider.CreateProvider("CSharp")
  let path = Path.Combine(outputDir, className + ".cs")
  use writer = new StreamWriter(path)
  let options = CodeGeneratorOptions()

  provider.GenerateCodeFromCompileUnit(cu, writer, options)

// Creates a single source file from the interpreted state
let createSingleFileCodeDom (state: InterpretedState) =
  printf "Generating code..."
  let cu = MakeCodeUnit state.entities state.intersections state.ns state.context

  cu |> generateFromCu state.outputDir "XrmContext"
  printfn "Done!"

// Creates multiple sources files from the interpreted state
let createMultiFileCodeDom (state: InterpretedState) =
  printf "Generating code..."
  let entities, globalSets = 
    state.entities 
    |> Array.map (fun entity -> MakeEntityCodeUnit entity state.ns)
    |> Array.unzip

  let intersects = state.intersections |> Array.map (fun intersect -> MakeIntersectCodeUnit intersect state.ns)
  let cu = Array.append entities intersects
  
  // Iterate through each code unit and generate source files
  cu |> Array.iter (fun (u, name) -> u |> generateFromCu state.outputDir name)
  
  // Generate global enums file
  let globalSets = 
    globalSets
    |> List.ofArray
    |> List.concat
    |> MakeEnumsCodeUnit state.ns
    |> generateFromCu state.outputDir "GlobalOptionSets"


  // Optionally generate context
  match state.context with
  | Some contextName -> 
    let contextCU, contextCUns = CreateStandardCodeUnit state.ns
    contextCUns.Types.Add(MakeContext state.entities contextName) |> ignore
    contextCU |> generateFromCu state.outputDir "XrmContext"
  | None -> ()

  printfn "Done!"

// Replace namespace in all lines
let replaceNamespace ns (lines:string seq) =
  if String.IsNullOrWhiteSpace ns then
    lines
  else 
    lines
    |> Seq.map (fun line -> line.Replace("DG.XrmContext", ns))

// Create resource files
let createResourceFiles out sdkVersion ns =
  getResourceLines "XrmExtensions.cs"
  |> removeUnsupportedLines sdkVersion
  |> replaceNamespace ns
  |> fun lines -> File.WriteAllLines(Path.Combine(out, "XrmExtensions.cs"), lines)