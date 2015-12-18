namespace DG.XrmContext

open DG.XrmContext
open GeneratorLogic

type ArgInfo = { command: string; description: string; required: bool }

type Args private () =
  static member expectedArgs = [
    { command="url";        required=true;  description="Url to the Organization.svc" }
    { command="username";   required=true;  description="CRM Username" }
    { command="password";   required=true;  description="CRM Password" }
    { command="domain";     required=false; description="Domain to use for CRM" }
    { command="ap";         required=false; description="Authentication Provider Type" }
    { command="out";        required=false; description="Output directory for the generated files" }
    { command="solutions";  required=false; description="Comma-separated list of solutions names. Generates code for the entities found in these solutions." }
    { command="entities";   required=false; 
      description=  "Comma-separated list of logical names of the entities it should generate code for. This is additive with the entities gotten via the \"solutions\" argument." }
    { command="namespace";  required=false; description="The namespace for the generated code. The default is the global namespace." }
    { command="servicecontextname";
                            required=false; description="The name of the generated organization service context class. If no value is supplied, no service context is created." }
    { command="deprecatedprefix";
                            required=false; description="Marks all attributes with the given prefix in their display name as deprecated." }
    ]

  // Usage
  static member usageString = 
    @"Usage: XrmContext.exe /url:http://<serverName>/<organizationName>/XRMServices/2011/Organization.svc /username:<username> /password:<password>"
  

  static member helpArgs = [ "help"; "-h"; "-help"; "--help"; "/h"; "/help" ] |> Set.ofList