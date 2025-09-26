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

        var interfaces = interfaceColumns.Select(@interface => IntersectionInterfaceMapper.MapToTemplateModel((@interface.Key, @interface.Value, (IList<TableModel>)tablesList), context)).ToList();

        // Add interface lists to tables (without modifying TableModel structure)
        var tablesWithInterfaces = tablesList.Select(table => ProxyClassMapper.MapToTemplateModel((table, tableToInterfaces.TryGetValue(table.LogicalName, out var iFaces) ? iFaces : new List<string>()), context)).ToList();

        // Prepare the template model with correct property names
        return new
        {
            @namespace = context.Namespace,
            version = context.Version,
            serviceContextName = string.IsNullOrEmpty(context.ServiceContextName) ? "Xrm" : context.ServiceContextName,
            tables = tablesWithInterfaces,
            optionsets = globalOptionsets,
            interfaces = interfaces,
        };
    }
}
