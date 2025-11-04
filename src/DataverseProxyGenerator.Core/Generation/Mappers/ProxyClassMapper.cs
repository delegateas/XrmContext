using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class ProxyClassMapper
{
    /// <summary>
    /// Non-virtual public members of Microsoft.Xrm.Sdk.Entity base class that would conflict with generated properties.
    /// These CANNOT be overridden and must be renamed if a column has the same name.
    /// Note: Virtual members like "Id" are NOT included here as they can be overridden without conflict.
    /// </summary>
    private static readonly HashSet<string> EntityBaseClassNonVirtualMembers = new(StringComparer.Ordinal)
    {
        // Non-virtual properties from Entity class
        "Attributes",
        "EntityState",
        "ExtensionData",
        "FormattedValues",
        "KeyAttributes",
        "LogicalName",
        "RelatedEntities",
        "RowVersion",
        "HasLazyFileAttribute",
        "LazyFileAttributeKey",
        "LazyFileAttributeValue",
        "LazyFileSizeAttributeKey",
        "LazyFileSizeAttributeValue",

        // Methods from Entity class (properties should not use these names either)
        "Contains",
        "GetAttributeValue",
        "GetFormattedAttributeValue",
        "GetRelatedEntities",
        "GetRelatedEntity",
        "SetAttributeValue",
        "SetRelatedEntities",
        "SetRelatedEntity",
        "ToEntity",
        "ToEntityReference",
        "TryGetAttributeValue",
        "ShallowCopyTo",
    };

    public static object MapToTemplateModel((TableModel Table, IReadOnlyList<string> Interfaces) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.Table);

        var (table, interfaces) = input;

        var processedColumns = ProcessColumnsWithClassNameConflictResolution(table.Columns, table.SchemaName);

        if (table.SchemaName == "EnvironmentVariableDefinition")
        {
            foreach (var key in table.Keys)
            {
                Console.WriteLine(key.SchemaName);
                foreach (var attr in key.KeyAttributes)
                {
                    Console.WriteLine($"    {attr.SchemaName} : {attr.TypeName}");
                }
            }
        }

        return new
        {
            SchemaName = table.SchemaName,
            Columns = processedColumns,
            Relationships = table.Relationships.Select(r => r with
            {
                SchemaName = GenerationUtilities.SanitizeName(r.SchemaName),
            }),
            Keys = table.Keys,
            LogicalName = table.LogicalName,
            DisplayName = table.DisplayName,
            Description = table.Description,
            EntityTypeCode = table.EntityTypeCode,
            PrimaryNameAttribute = table.PrimaryNameAttribute,
            PrimaryIdAttribute = table.PrimaryIdAttribute,
            IsIntersect = table.IsIntersect,
            InterfacesList = interfaces ?? new List<string>(),
        };
    }

    private static IEnumerable<ColumnModel> ProcessColumnsWithClassNameConflictResolution(IEnumerable<ColumnModel> columns, string className)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        return columns.Select(c =>
        {
            var sanitizedColumn = c switch
            {
                EnumColumnModel enumCol => enumCol with
                {
                    SchemaName = GenerationUtilities.SanitizeName(enumCol.SchemaName),
                    OptionsetName = GenerationUtilities.SanitizeName(enumCol.OptionsetName),
                },
                _ => c with
                {
                    SchemaName = GenerationUtilities.SanitizeName(c.SchemaName),
                },
            };

            var finalName = sanitizedColumn.SchemaName;

            // Check if sanitized schema name conflicts with class name (case-sensitive)
            if (string.Equals(finalName, className, StringComparison.Ordinal))
            {
                finalName = $"{finalName}_1";
            }

            // Check if name conflicts with non-virtual Entity base class members (case-sensitive)
            // Virtual members can be overridden, so they don't cause conflicts
            if (EntityBaseClassNonVirtualMembers.Contains(finalName))
            {
                finalName = $"{finalName}_1";
            }

            // Ensure the final name is unique (handle edge case where _1 suffix also conflicts)
            var candidateName = finalName;
            var suffix = 1;
            while (usedNames.Contains(candidateName))
            {
                suffix++;
                candidateName = $"{finalName}_{suffix}";
            }

            usedNames.Add(candidateName);

            return sanitizedColumn with { SchemaName = candidateName };
        });
    }
}