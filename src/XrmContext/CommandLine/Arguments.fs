namespace DG.XrmContext

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

    { command="deprecatedprefix";
      altCommands=["dp"]
      description="Marks all attributes with the given prefix in their display name as deprecated."
      required=false }

    { command="sdkversion";
      altCommands=["sv"]
      description="The version of the CrmSdk.CoreAssemblies which is used by your library. Automatically figures out the version from CRM if none is specified."
      required=false }

    { command="intersect";
      altCommands=["is"]
      description="Entities for which intersection interfaces should be created."
      required=false }
    ]

  static member connectionArgs = [
    { command="url"
      altCommands=[]
      description="Url to the Organization.svc"
      required=true }

    { command="username"
      altCommands=["u"; "usr"]
      description="CRM Username"
      required=true }

    { command="password"
      altCommands=["p"; "pwd"]
      description="CRM Password"
      required=true }

    { command="domain"
      altCommands=["d"; "dmn"]
      description="Domain to use for CRM"
      required=false }

    { command="ap"
      altCommands=[]
      description="Authentication Provider Type"
      required=false }
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
