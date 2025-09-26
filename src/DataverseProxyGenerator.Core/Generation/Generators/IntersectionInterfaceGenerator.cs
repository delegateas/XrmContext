using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class IntersectionInterfaceGenerator : BaseFileGenerator, IFileGenerator<(string InterfaceName, IEnumerable<ColumnSignature> Columns, IList<TableModel> Tables)>
{
    public IEnumerable<GeneratedFile> Generate((string InterfaceName, IEnumerable<ColumnSignature> Columns, IList<TableModel> Tables) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.InterfaceName);
        ArgumentNullException.ThrowIfNull(input.Tables);
        return GenerateInternal(input, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal((string InterfaceName, IEnumerable<ColumnSignature> Columns, IList<TableModel> Tables) input, GenerationContext context)
    {
        ValidateContext(context);

        var intersectionModel = IntersectionInterfaceMapper.MapToTemplateModel(input, context);
        var templateModel = new
        {
            @interface = intersectionModel,
            @namespace = context.Namespace,
            version = context.Version,
        };

        var template = context.Templates.GetTemplate("IntersectionInterface.scriban-cs");

        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var interfaceResult = template.Render(templateContext);

        var sanitizedInterfaceName = GenerationUtilities.SanitizeName(input.InterfaceName);
        yield return new GeneratedFile(FilePathHelper.GetIntersectionInterfaceFilePath(sanitizedInterfaceName), interfaceResult);
    }
}