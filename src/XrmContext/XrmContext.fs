namespace DG.XrmContext

open System
open Utility
open GeneratorLogic
open System.Diagnostics
open System.IO

type XrmContext private () =

  static member GetContext(url, username, password, ?domain, ?ap, ?out, ?entities, ?solutions, ?ns, ?context, ?deprecatedPrefix, ?sdkVersion) =
    let xrmAuth =
      { XrmAuthentication.url = Uri(url)
        username = username
        password = password
        domain = domain
        ap = ap
      }

    let settings = 
      { XrmContextSettings.out = out
        ns = ns
        context = context
        solutions = solutions
        entities = entities
        deprecatedPrefix = deprecatedPrefix
        sdkVersion = sdkVersion
      }
    
    XrmContext.GetContext(xrmAuth, settings)


  static member GetContext(xrmAuth, settings) =
    #if !DEBUG
    try
    #endif
      let out = settings.out |? "."
      let ns = settings.ns |? null

      // Connect to CRM and interpret the data
      let proxy = connectToCrm xrmAuth
      
      let proxyGetter = proxyHelper xrmAuth
      let entities = 
        getFullEntityList settings.entities settings.solutions proxy
      
      let sdkVersion =
        if settings.sdkVersion.IsNone then retrieveCrmVersion proxy
        else settings.sdkVersion.Value

      let data = 
        (proxy, proxyGetter)
        ||> retrieveCrmData entities
        |> interpretCrmData out ns settings.deprecatedPrefix settings.context sdkVersion

      // Generate the code
      createCodeDom data
      createResourceFiles sdkVersion out

      printfn "\nDone generating context!"

    #if !DEBUG
    with 
      | :? AggregateException as ex ->
        failwithf "\nUnable to generate context: %s" ex.InnerException.Message
      | _ as ex ->
        failwithf "\nUnable to generate context: %s" ex.Message
    #endif

