﻿namespace DG.XrmContext

open DG.XrmContext
open System.Configuration

type ArgInfo = 
  { command: string
    altCommands: string list
    description: string
    required: bool 
  }

type Args private () =
  static member generationArgs = [
    { command="out"
      altCommands=["o"]
      description="Output directory for the generated files"
      required=false }

    { command="solutions"
      altCommands=["ss"]
      description="Comma-separated list of solutions names. Generates code for the entities found in these solutions."
      required=false }

    { command="entities"
      altCommands=["es"]
      description="Comma-separated list of logical names of the entities it should generate code for. This is additive with the entities gotten via the \"solutions\" argument."
      required=false }

    { command="namespace"
      altCommands=["ns"]
      description="The namespace for the generated code. The default is the global namespace."
      required=false }

    { command="servicecontextname"
      altCommands=["scn"]
      description="The name of the generated organization service context class. If no value is supplied, no service context is created."
      required=false }

    { command="deprecatedPrefix";
      altCommands=["dp"]
      description="Marks all attributes with the given prefix in their display name as deprecated."
      required=false }

    { command="sdkVersion";
      altCommands=["sv"]
      description="The version of the CrmSdk.CoreAssemblies which is used by your library. Automatically figures out the version from CRM if none is specified."
      required=false }

    { command="intersect";
      altCommands=["is"]
      description="Entities for which intersection interfaces should be created."
      required=false }

    { command="labelMappings";
      altCommands=["lm"]
      description="Labels unicode characters that should not be displayed in code and instead be represented by a different string: Example ✔️ to Checkmark."
      required=false }

    { command="oneFile";
      altCommands=["of"]
      description="Set to false to generate one file per entity instead of one big file."
      required=false }

    { command = "includeEntityTypeCode";
      altCommands=["ietc"]
      description = "Set to false to prevent generation of an Entity Type Code constant in each entity definition. Default is true."
      required=false }

    { command = "localizations";
      altCommands=["l"]
      description="Comma seperated list of LCIDs to include as OptionSetMetadataAttribute the, default is to use UserLocalizedLabel."
      required=false }
    ]

  static member connectionArgs = [
    { command="url"
      altCommands=[]
      description="Url to the Organization.svc"
      required=true }

    { command="method"
      altCommands=[]
      description="Connection method"
      required=false }

    { command="username"
      altCommands=["u"; "usr"]
      description="CRM Username"
      required=false }

    { command="password"
      altCommands=["p"; "pwd"]
      description="CRM Password"
      required=false }

    { command="domain"
      altCommands=["d"; "dmn"]
      description="Domain to use for CRM"
      required=false }

    { command="ap"
      altCommands=[]
      description="Authentication Provider Type"
      required=false }

    { command="mfaAppId"
      altCommands=[]
      description="Azure Application Id"
      required=false }
    
    { command="mfaReturnUrl"
      altCommands=[]
      description="Return URL defined for the Azure Application"
      required=false }

    { command="mfaClientSecret"
      altCommands=[]
      description="Client secret for the Azure Application"
      required=false }

    { command="connectionString"
      altCommands=[]
      description="Connection string used for authentication"
      required=false }
    {
        command="prompt"
        altCommands=[]
        description="When should the authentication prompt be shown to the user (Auto (default), Always, RefreshSession, Never, SelectAccount)"
        required=false
    }
  ]

  (** Special arguments, which make the program act differently than normal *)
  static member saveFlag = 
    { command="save"
      altCommands=[]
      description="Flag to indicate to retrieve the metadata and store it in a file."
      required=false }

  static member loadFlag = 
    { command="load"
      altCommands=[]
      description="Flag to indicate to load the metadata from a local file instead of contacting CRM."
      required=false }

  static member genConfigFlag = 
    { command="genconfig"
      altCommands=["gc"]
      description="Flag to indicate that a dummy configuration file should be generated."
      required=false }

  static member flagArgs = [
    Args.saveFlag
    Args.loadFlag
    Args.genConfigFlag
  ] 

  static member useConfig = 
    { command="useconfig"
      altCommands=["uc"]
      description="Flag to indicate that it should use the given configuration along with command-line arguments."
      required=false }




  static member useConfigSet = Args.useConfig.command :: Args.useConfig.altCommands |> Set.ofList

  static member makeArgMap argList = 
    argList
    |> Seq.fold (fun acc argInfo -> 
        argInfo.command :: argInfo.altCommands 
        |> List.fold (fun innerAcc arg -> 
          (arg.ToLower(), argInfo) :: innerAcc) acc) 
      []
    |> Map.ofSeq

  static member flagArgMap = Args.makeArgMap Args.flagArgs

  static member fullArgList = List.concat [ Args.connectionArgs; Args.generationArgs; Args.flagArgs; [Args.useConfig] ]
  static member argMap = Args.makeArgMap Args.fullArgList
  
  static member helpArgs = [ "?"; "help"; "-h"; "-help"; "--help"; "/h"; "/help" ] |> Set.ofList


  // Usage
  static member usageString = 
    @"Usage: XrmContext.exe /url:http://<serverName>/<organizationName>/XRMServices/2011/Organization.svc /username:<username> /password:<password>"
  

  static member genConfig () =
    let configmanager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
    let config = configmanager.AppSettings.Settings
    config.Add("url", "https://INSTANCE.crm4.dynamics.com/XRMServices/2011/Organization.svc")
    config.Add("username","admin@INSTANCE.onmicrosoft.com")
    config.Add("password", "pass@word1")
    configmanager.Save(ConfigurationSaveMode.Modified)
    printfn "Generated configuration file with dummy values. Change them to fit your environment."
