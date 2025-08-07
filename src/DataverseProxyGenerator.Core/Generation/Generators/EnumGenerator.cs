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
        var sanitizedOptionSetName = SanitizeName(input.OptionsetName, "UnknownOptionSet");

        // Generate unique enum member names to handle duplicate labels using groupBy approach
        var sanitizedOptions = input.OptionsetValues
            .Select(kvp => new { kvp.Key, kvp.Value, SanitizedName = NameSanitizer.SanitizeEnumOptionName(kvp.Value, kvp.Key) })
            .ToList();

        var optionsetValuesWithUniqueNames = sanitizedOptions
            .GroupBy(item => item.SanitizedName, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group =>
            {
                var items = group.ToList();
                return items.Select((item, index) => new
                {
                    Value = item.Key,
                    Name = index == 0 ? item.SanitizedName : $"{item.SanitizedName}_{index}",
                    Localizations =
                        input.OptionLocalizations != null &&
                        input.OptionLocalizations.TryGetValue(item.Key, out var value)
                        ? value : new Dictionary<int, string>(),
                });
            })
            .ToList();

        var enumResult = template.Render(
            new
            {
                optionsetName = sanitizedOptionSetName,
                optionsetValues = optionsetValuesWithUniqueNames,
                @namespace = context.Namespace,
                version = context.Version,
            },
            member => member.Name);

        yield return new GeneratedFile(FilePathHelper.GetOptionSetFilePath(sanitizedOptionSetName), enumResult);
    }
}