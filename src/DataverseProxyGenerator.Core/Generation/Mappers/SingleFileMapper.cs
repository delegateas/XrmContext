using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class SingleFileMapper
{
    public static object MapToTemplateModel(
        IReadOnlyList<TableModel> tablesList,
        IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> interfaceColumns,
        IReadOnlyDictionary<string, IReadOnlyList<string>> tableToInterfaces,
        GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tablesList);
        ArgumentNullException.ThrowIfNull(interfaceColumns);
        ArgumentNullException.ThrowIfNull(tableToInterfaces);

        var globalOptionsets = GenerationUtilities.GetGlobalOptionsets(tablesList)
            .Select(enumCol => EnumMapper.MapToTemplateModel(enumCol, context))
            .ToList();
        var interfaces = CreateInterfaceModels(interfaceColumns, tablesList);

        // Add interface lists to tables (without modifying TableModel structure)
        var tablesWithInterfaces = tablesList.Select(table =>
        {
            var tableInterfaces = tableToInterfaces.TryGetValue(table.LogicalName, out var ifaces) ? ifaces : new List<string>();
            return new
            {
                table,
                InterfacesList = tableInterfaces,
            };
        }).ToList();

        // Prepare the template model with correct property names
        return new
        {
            @namespace = context.Namespace,
            version = context.Version,
            serviceContextName = string.IsNullOrEmpty(context.ServiceContextName) ? "Xrm" : context.ServiceContextName,
            tables = tablesWithInterfaces.Select(t => new
            {
                t.table.SchemaName,
                t.table.LogicalName,
                t.table.DisplayName,
                t.table.Description,
                t.table.EntityTypeCode,
                t.table.PrimaryNameAttribute,
                t.table.PrimaryIdAttribute,
                t.table.IsIntersect,
                t.table.Columns,
                t.table.Relationships,
                InterfacesList = t.InterfacesList,
            }).ToList(),
            optionsets = globalOptionsets,
            interfaces = interfaces,
        };
    }

    private static List<object> CreateInterfaceModels(
        IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> interfaceColumns,
        IReadOnlyList<TableModel> tablesList)
    {
        return interfaceColumns.Select(kvp => new
        {
            Name = kvp.Key,
            Columns = kvp.Value.Select(sig => FindMatchingColumn(sig, tablesList))
                .Where(c => c != null)
                .Select(col => new
                {
                    SchemaName = GenerationUtilities.SanitizeName(col!.SchemaName),
                    col.DisplayName,
                    col.Description,
                    TypeSignature = GenerationUtilities.GetTypeSignature(col),
                })
                .ToList(),
        }).ToList<object>();
    }

    private static ColumnModel? FindMatchingColumn(ColumnSignature sig, IReadOnlyList<TableModel> tablesList)
    {
        return tablesList.SelectMany(t => t.Columns)
            .FirstOrDefault(c => c.SchemaName == sig.SchemaName &&
                (c.TypeName == sig.TypeName ||
                 (c is EnumColumnModel enumCol && sig.TypeName == $"EnumColumnModel:{enumCol.OptionsetName}")));
    }
}
