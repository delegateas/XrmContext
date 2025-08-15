using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class EnumMapper
{
    public static object MapToTemplateModel(EnumColumnModel input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        var sanitizedOptionSetName = DataverseProxyGenerator.Core.Generation.Utilities.GenerationUtilities.SanitizeName(input.OptionsetName, "UnknownOptionSet");

        return new
        {
            optionsetName = sanitizedOptionSetName,
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
        };
    }
}
