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
        var sanitizedUniqueName = GenerationUtilities.SanitizeName(customApi.UniqueName);
        var requestFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{sanitizedUniqueName}Request.cs");

        files.Add(new GeneratedFile(requestFilename, requestContent));

        // Generate response class
        var responseTemplate = context.Templates.GetTemplate("CustomApiResponse.scriban-cs");
        var responseModel = CreateTemplateModel(customApi, context);
        var responseContent = responseTemplate.Render(responseModel);
        var responseFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{sanitizedUniqueName}Response.cs");

        files.Add(new GeneratedFile(responseFilename, responseContent));

        return files;
    }

    private static object CreateTemplateModel(CustomApiModel customApi, GenerationContext context)
    {
        return new
        {
            unique_name = customApi.UniqueName, // Original for RequestName/ResponseName
            sanitized_unique_name = GenerationUtilities.SanitizeName(customApi.UniqueName), // Sanitized for class names
            display_name = customApi.DisplayName,
            description = customApi.Description,
            is_function = customApi.IsFunction,
            version = context.Version,
            @namespace = context.Namespace,
            request_parameters = customApi.RequestParameters.Select(p => new
            {
                name = GenerationUtilities.SanitizeName(p.Name), // Sanitized for C# property names
                original_name = p.Name, // Original for Parameters collection access
                unique_name = p.UniqueName,
                display_name = p.DisplayName,
                description = p.Description,
                csharp_type = GetCSharpType(p.Type),
                is_optional = p.IsOptional,
                logical_entity_name = p.LogicalEntityName,
                xml_doc_comment = GetXmlDocComment(p.Description),
            }).ToList(),
            response_properties = customApi.ResponseProperties.Select(p => new
            {
                name = GenerationUtilities.SanitizeName(p.Name), // Sanitized for C# property names
                original_name = p.Name, // Original for Results collection access
                unique_name = p.UniqueName,
                display_name = p.DisplayName,
                description = p.Description,
                csharp_type = GetCSharpType(p.Type),
                is_optional = p.IsOptional,
                logical_entity_name = p.LogicalEntityName,
                xml_doc_comment = GetXmlDocComment(p.Description),
            }).ToList(),
        };
    }

    private static string GetCSharpType(CustomApiParameterType type)
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

        return $"/// <summary>\n/// {description}\n/// </summary>";
    }
}