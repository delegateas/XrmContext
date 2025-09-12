using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class HelperFileGenerator : BaseFileGenerator, IFileGenerator<string>
{
    public IEnumerable<GeneratedFile> Generate(string templateName, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(templateName);
        return GenerateInternal(templateName, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal(string templateName, GenerationContext context)
    {
        ValidateContext(context);

        var templateModel = HelperFileMapper.MapToTemplateModel(templateName, context);
        var template = context.Templates.GetTemplate($"{templateName}.scriban-cs");
        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var result = template.Render(templateContext);

        yield return new GeneratedFile(FilePathHelper.GetHelperFilePath(templateName), result);
    }
}