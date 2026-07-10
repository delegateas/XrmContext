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
                name = GenerationUtilities.SanitizeName(p.UniqueName),
                original_name = p.Name,
                unique_name = p.UniqueName,
                display_name = p.DisplayName,
                description = p.Description,
                csharp_type = GetCSharpType(p.Type, p.IsOptional, context.NullableTypes),
                is_optional = p.IsOptional,
                logical_entity_name = p.LogicalEntityName,
                xml_doc_comment = GetXmlDocComment(p.Description),
            }).ToList(),
            response_properties = customApi.ResponseProperties.Select(p => new
            {
                name = GenerationUtilities.SanitizeName(p.UniqueName),
                original_name = p.Name,
                unique_name = p.UniqueName,
                display_name = p.DisplayName,
                argument_name = string.Concat(p.UniqueName[..1].ToLowerInvariant(), p.UniqueName.AsSpan(1)),
                description = p.Description,
                csharp_type = GetCSharpType(p.Type, p.IsOptional, context.NullableTypes),
                is_optional = p.IsOptional,
                logical_entity_name = p.LogicalEntityName,
                xml_doc_comment = GetXmlDocComment(p.Description),
            }).ToList(),
        };
    }

    public static string GetCSharpType(CustomApiParameterType type, bool isOptional, bool nullableTypes)
    {
        return type switch
        {
            CustomApiParameterType.BooleanType => isOptional ? "bool?" : "bool",
            CustomApiParameterType.DateTimeType => isOptional ? "System.DateTime?" : "System.DateTime",
            CustomApiParameterType.DecimalType => isOptional ? "decimal?" : "decimal",
            CustomApiParameterType.EntityType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.Entity?" : "Microsoft.Xrm.Sdk.Entity",
            CustomApiParameterType.EntityCollectionType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.EntityCollection?" : "Microsoft.Xrm.Sdk.EntityCollection",
            CustomApiParameterType.EntityReferenceType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.EntityReference?" : "Microsoft.Xrm.Sdk.EntityReference",
            CustomApiParameterType.FloatType => isOptional ? "double?" : "double",
            CustomApiParameterType.IntegerType => isOptional ? "int?" : "int",
            CustomApiParameterType.MoneyType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.Money?" : "Microsoft.Xrm.Sdk.Money",
            CustomApiParameterType.PicklistType => isOptional && nullableTypes ? "Microsoft.Xrm.Sdk.OptionSetValue?" : "Microsoft.Xrm.Sdk.OptionSetValue",
            CustomApiParameterType.StringType => isOptional && nullableTypes ? "string?" : "string",
            CustomApiParameterType.StringArrayType => isOptional && nullableTypes ? "string[]?" : "string[]",
            CustomApiParameterType.GuidType => isOptional ? "System.Guid?" : "System.Guid",
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