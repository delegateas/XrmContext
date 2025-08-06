using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
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

        var template = context.Templates.GetTemplate("EnumOptionset.scriban-cs");

        var enumResult = template.Render(
            new
            {
                optionsetName = SanitizeName(input.OptionsetName, "UnknownOptionSet"),
                optionsetValues = input.OptionsetValues.Select(kvp => new
                {
                    Value = kvp.Key,
                    Name = NameSanitizer.SanitizeEnumOptionName(kvp.Value, kvp.Key),
                    Localizations =
                        input.OptionLocalizations != null &&
                        input.OptionLocalizations.TryGetValue(kvp.Key, out var value)
                        ? value : new Dictionary<int, string>(),
                }),
                @namespace = context.Namespace,
                version = context.Version,
            },
            member => member.Name);

        yield return new GeneratedFile(FilePathHelper.GetOptionSetFilePath(input.OptionsetName), enumResult);
    }
}