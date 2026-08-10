using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class EnumGenerator : BaseFileGenerator, IFileGenerator<EnumColumnModel>
{
    public IAsyncEnumerable<GeneratedFile> GenerateAsync(EnumColumnModel input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);
        return GenerateInternalAsync(input, context);
    }

    private static async IAsyncEnumerable<GeneratedFile> GenerateInternalAsync(EnumColumnModel input, GenerationContext context)
    {
        ValidateContext(context);

        var optionsetModel = EnumMapper.MapToTemplateModel(input, context);
        var templateModel = new
        {
            optionset = optionsetModel,
            @namespace = context.Namespace,
            version = context.Version,
        };

        var template = await context.Templates.GetTemplateAsync("EnumOptionset.scriban-cs");
        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var enumResult = await template.RenderAsync(templateContext);

        var sanitizedOptionSetName = GenerationUtilities.SanitizeName(input.OptionsetName, "UnknownOptionSet");
        yield return new GeneratedFile(FilePathHelper.GetOptionSetFilePath(sanitizedOptionSetName), enumResult);
    }
}