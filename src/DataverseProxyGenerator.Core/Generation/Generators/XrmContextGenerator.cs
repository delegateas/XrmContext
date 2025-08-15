using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class XrmContextGenerator : BaseFileGenerator, IFileGenerator<IEnumerable<TableModel>>
{
    public IEnumerable<GeneratedFile> Generate(IEnumerable<TableModel> input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);
        return GenerateInternal(input, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal(IEnumerable<TableModel> input, GenerationContext context)
    {
        ValidateContext(context);

        var templateModel = XrmContextMapper.MapToTemplateModel(input, context);
        var template = context.Templates.GetTemplate("XrmClass.scriban-cs");
        var xrmClassResult = template.Render(templateModel, member => member.Name);

        yield return new GeneratedFile(FilePathHelper.GetXrmContextFilePath(), xrmClassResult);
    }
}