using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class ProxyClassMapper
{
    /// <summary>
    /// The list is limited to properties where collisions have been identified.
    /// </summary>
    private static readonly HashSet<string> RestrictedAttributeNames = new(StringComparer.Ordinal)
    {
        "Attributes", // Collision on SdkMessageProcessingStepImage
    };

    public static object MapToTemplateModel((TableModel Table, IReadOnlyList<string> Interfaces) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.Table);

        var (table, interfaces) = input;

        var processedColumns = ProcessColumnsWithNameConflictResolution(table.Columns, table.SchemaName);

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

    private static IEnumerable<ColumnModel> ProcessColumnsWithNameConflictResolution(IEnumerable<ColumnModel> columns, string className)
    {
        var usedNames = new HashSet<string>(RestrictedAttributeNames, StringComparer.Ordinal);
        usedNames.Add(className);

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

            var defaultName = sanitizedColumn.SchemaName;

            // Ensure the final name is unique (handle edge case where _1 suffix also conflicts)
            var candidateName = defaultName;
            var suffix = 0;
            while (usedNames.Contains(candidateName))
            {
                suffix++;
                candidateName = $"{defaultName}_{suffix}";
            }

            usedNames.Add(candidateName);

            return sanitizedColumn with { SchemaName = candidateName };
        });
    }
}