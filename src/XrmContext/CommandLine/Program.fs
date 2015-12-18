
open System
open System.Text.RegularExpressions

open Microsoft.Xrm.Sdk.Client

open DG.XrmContext
open CommandLineHelper
open GeneratorLogic


// Main executable function
let executeGetContext argv =
  let parsedArgs = parseArgs argv Args.expectedArgs

  if parsedArgs.Count = 0 then showUsage()
  else

  let ap = 
    getArg parsedArgs "ap" 
      (fun ap -> Enum.Parse(typeof<AuthenticationProviderType>, ap) :?> AuthenticationProviderType)

  let xrmAuth =
    { XrmAuthentication.url = Uri(parsedArgs.Item "url")
      username = parsedArgs.Item "username"
      password = parsedArgs.Item "password"
      domain = parsedArgs.TryFind "domain"
      ap = ap }

  let entities = getListArg parsedArgs "entities" (fun s -> s.ToLower())
  let solutions = getListArg parsedArgs "solutions" id

  let settings = 
    { XrmContextSettings.out = parsedArgs.TryFind "out"
      ns = parsedArgs.TryFind "namespace"
      context = parsedArgs.TryFind "servicecontextname"
      entities = entities
      solutions = solutions
      deprecatedPrefix = parsedArgs.TryFind "deprecatedprefix"
    }
  
  XrmContext.GetContext(xrmAuth, settings)


// Main method
[<EntryPoint>]
let main argv = 
  #if DEBUG
  executeGetContext argv
  0
  #else
  try 
    if argv.Length > 0 && Args.helpArgs.Contains argv.[0] then showUsage()
    else executeGetContext argv
  with ex ->
    eprintfn "%s" ex.Message
  0
  #endif
