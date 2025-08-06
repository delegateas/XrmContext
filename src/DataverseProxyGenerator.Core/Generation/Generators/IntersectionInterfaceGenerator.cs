using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class IntersectionInterfaceGenerator : BaseFileGenerator, IFileGenerator<(string InterfaceName, IEnumerable<ColumnModel> Columns)>
{
    public IEnumerable<GeneratedFile> Generate((string InterfaceName, IEnumerable<ColumnModel> Columns) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.InterfaceName);
        return GenerateInternal(input, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal((string InterfaceName, IEnumerable<ColumnModel> Columns) input, GenerationContext context)
    {
        ValidateContext(context);
        var (interfaceName, columns) = input;
        var template = context.Templates.GetTemplate("IntersectionInterface.scriban-cs");

        var columnData = columns.Select(col => new
        {
            SchemaName = SanitizeName(col.SchemaName),
            col.DisplayName,
            col.Description,
            TypeSignature = GetTypeSignature(col),
        });

        var interfaceResult = template.Render(
            new
            {
                interfaceName,
                @namespace = context.Namespace,
                columns = columnData,
                version = context.Version,
            },
            member => member.Name);

        yield return new GeneratedFile(FilePathHelper.GetIntersectionInterfaceFilePath(interfaceName), interfaceResult);
    }
}