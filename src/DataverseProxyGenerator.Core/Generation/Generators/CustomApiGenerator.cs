using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class CustomApiGenerator : BaseFileGenerator, IFileGenerator<CustomApiModel>
{
    public IEnumerable<GeneratedFile> Generate(CustomApiModel customApi, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(customApi);
        ArgumentNullException.ThrowIfNull(context);
        ValidateContext(context);

        var files = new List<GeneratedFile>();

        // Generate request class
        var requestTemplate = context.Templates.GetTemplate("CustomApiRequest.scriban-cs");
        var requestModel = CreateTemplateModel(customApi, context);
        var requestContent = requestTemplate.Render(requestModel);
        var requestFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{customApi.UniqueName}Request.cs");

        files.Add(new GeneratedFile(requestFilename, requestContent));

        // Generate response class
        var responseTemplate = context.Templates.GetTemplate("CustomApiResponse.scriban-cs");
        var responseModel = CreateTemplateModel(customApi, context);
        var responseContent = responseTemplate.Render(responseModel);
        var responseFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{customApi.UniqueName}Response.cs");

        files.Add(new GeneratedFile(responseFilename, responseContent));

        return files;
    }

    private static object CreateTemplateModel(CustomApiModel customApi, GenerationContext context)
    {
        return new
        {
            unique_name = customApi.UniqueName,
            display_name = customApi.DisplayName,
            description = customApi.Description,
            is_function = customApi.IsFunction,
            version = context.Version,
            @namespace = context.Namespace,
            request_parameters = customApi.RequestParameters,
            response_properties = customApi.ResponseProperties,
            get_csharp_type = new Func<CustomApiParameterType, string?, string>(GetCSharpType),
            get_xml_doc_comment = new Func<string, string>(GetXmlDocComment),
        };
    }

    private static string GetCSharpType(CustomApiParameterType type, string? logicalEntityName)
    {
        return type switch
        {
            CustomApiParameterType.BooleanType => "bool",
            CustomApiParameterType.DateTimeType => "System.DateTime",
            CustomApiParameterType.DecimalType => "decimal",
            CustomApiParameterType.EntityType => "Microsoft.Xrm.Sdk.Entity",
            CustomApiParameterType.EntityCollectionType => "Microsoft.Xrm.Sdk.EntityCollection",
            CustomApiParameterType.EntityReferenceType => "Microsoft.Xrm.Sdk.EntityReference",
            CustomApiParameterType.FloatType => "double",
            CustomApiParameterType.IntegerType => "int",
            CustomApiParameterType.MoneyType => "Microsoft.Xrm.Sdk.Money",
            CustomApiParameterType.PicklistType => "Microsoft.Xrm.Sdk.OptionSetValue",
            CustomApiParameterType.StringType => "string",
            CustomApiParameterType.StringArrayType => "string[]",
            CustomApiParameterType.GuidType => "System.Guid",
            _ => "object",
        };
    }

    private static string GetXmlDocComment(string description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        return $"/// <summary>\n    /// {description}\n    /// </summary>";
    }
}