using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class IntersectionInterfaceMapper
{
    public static object MapToTemplateModel((string InterfaceName, IEnumerable<ColumnModel> Columns) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.InterfaceName);

        var (interfaceName, columns) = input;
        var sanitizedInterfaceName = GenerationUtilities.SanitizeName(interfaceName);

        var columnData = columns.Select(col => new
        {
            SchemaName = GenerationUtilities.SanitizeName(col.SchemaName),
            col.DisplayName,
            col.Description,
            TypeSignature = GenerationUtilities.GetTypeSignature(col),
        });

        return new
        {
            @interface = new
            {
                Name = sanitizedInterfaceName,
                Columns = columnData,
            },
            @namespace = context.Namespace,
            version = context.Version,
        };
    }
}