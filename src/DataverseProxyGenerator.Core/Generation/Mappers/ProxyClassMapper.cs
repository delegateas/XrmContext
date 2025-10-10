using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class ProxyClassMapper
{
    public static object MapToTemplateModel((TableModel Table, IReadOnlyList<string> Interfaces) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.Table);

        var (table, interfaces) = input;

        var processedColumns = ProcessColumnsWithClassNameConflictResolution(table.Columns, table.SchemaName);

        return new
        {
            SchemaName = table.SchemaName,
            Columns = processedColumns,
            Relationships = table.Relationships.Select(r => r with
            {
                SchemaName = GenerationUtilities.SanitizeName(r.SchemaName),
            }),
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

            // Check if sanitized schema name conflicts with class name (case-sensitive)
            if (string.Equals(sanitizedColumn.SchemaName, className, StringComparison.Ordinal))
            {
                var finalName = $"{sanitizedColumn.SchemaName}_1";
                return sanitizedColumn with { SchemaName = finalName };
            }

            return sanitizedColumn;
        });
    }
}