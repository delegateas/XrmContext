using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class XrmContextGenerator : BaseFileGenerator, IFileGenerator<IEnumerable<TableModel>>
{
    public IAsyncEnumerable<GeneratedFile> GenerateAsync(IEnumerable<TableModel> input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);
        return GenerateInternalAsync(input, context);
    }

    private static async IAsyncEnumerable<GeneratedFile> GenerateInternalAsync(IEnumerable<TableModel> input, GenerationContext context)
    {
        ValidateContext(context);

        var serviceContextName = context.ServiceContextName ?? "Xrm";

        var templateModel = XrmContextMapper.MapToTemplateModel(input, context);
        var template = await context.Templates.GetTemplateAsync("XrmClass.scriban-cs");
        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var xrmClassResult = await template.RenderAsync(templateContext);

        yield return new GeneratedFile(FilePathHelper.GetXrmContextFilePath(serviceContextName), xrmClassResult);
    }
}