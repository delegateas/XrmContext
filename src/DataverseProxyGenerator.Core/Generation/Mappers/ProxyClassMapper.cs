using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class ProxyClassMapper
{
    public static object MapToTemplateModel((TableModel Table, IReadOnlyList<string> Interfaces) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.Table);

        var (table, interfaces) = input;

        return new
        {
            table = new
            {
                SchemaName = table.SchemaName,
                Columns = table.Columns.Select(c =>
                c switch
                {
                    EnumColumnModel enumCol => enumCol with
                    {
                        SchemaName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(enumCol.SchemaName),
                        OptionsetName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(enumCol.OptionsetName),
                    },
                    _ => c with
                    {
                        SchemaName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(c.SchemaName),
                    },
                }),
                Relationships = table.Relationships.Select(r => r with
                {
                    SchemaName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(r.SchemaName),
                }),
                LogicalName = table.LogicalName,
                DisplayName = table.DisplayName,
                EntityTypeCode = table.EntityTypeCode,
                PrimaryNameAttribute = table.PrimaryNameAttribute,
                PrimaryIdAttribute = table.PrimaryIdAttribute,
                IsIntersect = table.IsIntersect,
                InterfacesList = interfaces ?? new List<string>(),
            },
            @namespace = context.Namespace,
            version = context.Version,
        };
    }
}