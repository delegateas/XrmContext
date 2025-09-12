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

        var serviceContextName = context.ServiceContextName ?? "Xrm";

        var templateModel = XrmContextMapper.MapToTemplateModel(input, context);
        var template = context.Templates.GetTemplate("XrmClass.scriban-cs");
        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var xrmClassResult = template.Render(templateContext);

        yield return new GeneratedFile(FilePathHelper.GetXrmContextFilePath(serviceContextName), xrmClassResult);
    }
}