using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class HelperFileGenerator : BaseFileGenerator, IFileGenerator<string>
{
    public IAsyncEnumerable<GeneratedFile> GenerateAsync(string templateName, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(templateName);
        return GenerateInternalAsync(templateName, context);
    }

    private static async IAsyncEnumerable<GeneratedFile> GenerateInternalAsync(string templateName, GenerationContext context)
    {
        ValidateContext(context);

        var templateModel = HelperFileMapper.MapToTemplateModel(templateName, context);
        var template = await context.Templates.GetTemplateAsync($"{templateName}.scriban-cs");
        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var result = await template.RenderAsync(templateContext);

        yield return new GeneratedFile(FilePathHelper.GetHelperFilePath(templateName), result);
    }
}