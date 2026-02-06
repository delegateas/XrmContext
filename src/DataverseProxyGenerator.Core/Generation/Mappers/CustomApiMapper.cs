using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class CustomApiMapper
{
    public static object MapToTemplateModel(CustomApiModel customApi, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(customApi);
        ArgumentNullException.ThrowIfNull(context);

        return new
        {
            unique_name = customApi.UniqueName,
            sanitized_unique_name = GenerationUtilities.SanitizeName(customApi.UniqueName),
            display_name = customApi.DisplayName,
            description = customApi.Description,
            is_function = customApi.IsFunction,
            request_parameters = customApi.RequestParameters.Select(p => new
            {
                name = GenerationUtilities.SanitizeName(p.Name),
                original_name = p.Name,
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
                name = GenerationUtilities.SanitizeName(p.Name),
                original_name = p.Name,
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

    public static string GetCSharpType(CustomApiParameterType type)
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

    public static string GetXmlDocComment(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        return $"/// <summary>\n/// {description}\n/// </summary>";
    }
}