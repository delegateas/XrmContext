using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
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

        var templateModel = IntersectionInterfaceMapper.MapToTemplateModel(input, context);
        var template = context.Templates.GetTemplate("IntersectionInterface.scriban-cs");
        var interfaceResult = template.Render(templateModel, member => member.Name);

        var sanitizedInterfaceName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(input.InterfaceName);
        yield return new GeneratedFile(FilePathHelper.GetIntersectionInterfaceFilePath(sanitizedInterfaceName), interfaceResult);
    }
}