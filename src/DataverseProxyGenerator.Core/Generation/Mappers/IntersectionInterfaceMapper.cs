using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class IntersectionInterfaceMapper
{
    public static object MapToTemplateModel((string InterfaceName, IEnumerable<ColumnModel> Columns) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.InterfaceName);

        var (interfaceName, columns) = input;
        var sanitizedInterfaceName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(interfaceName);

        var columnData = columns.Select(col => new
        {
            SchemaName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(col.SchemaName),
            col.DisplayName,
            col.Description,
            TypeSignature = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.GetTypeSignature(col),
        });

        return new
        {
            interfaceName = sanitizedInterfaceName,
            @namespace = context.Namespace,
            columns = columnData,
            version = context.Version,
        };
    }
}
