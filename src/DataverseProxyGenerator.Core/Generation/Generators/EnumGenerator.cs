using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class EnumGenerator : BaseFileGenerator, IFileGenerator<EnumColumnModel>
{
    public IEnumerable<GeneratedFile> Generate(EnumColumnModel input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);
        return GenerateInternal(input, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal(EnumColumnModel input, GenerationContext context)
    {
        ValidateContext(context);

        var templateModel = EnumMapper.MapToTemplateModel(input, context);
        var template = context.Templates.GetTemplate("EnumOptionset.scriban-cs");
        var enumResult = template.Render(templateModel, member => member.Name);

        var sanitizedOptionSetName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(input.OptionsetName, "UnknownOptionSet");
        yield return new GeneratedFile(FilePathHelper.GetOptionSetFilePath(sanitizedOptionSetName), enumResult);
    }
}