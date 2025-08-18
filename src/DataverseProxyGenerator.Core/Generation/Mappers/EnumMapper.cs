using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class EnumMapper
{
    public static object MapToTemplateModel(EnumColumnModel input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        var sanitizedOptionSetName = GenerationUtilities.SanitizeName(input.OptionsetName, "UnknownOptionSet");

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
                        ? value.Select( kvp => new KeyValuePair<int, string>(kvp.Key, NameSanitizer.SanitizeString(kvp.Value)))
                        : new Dictionary<int, string>(),
                });
            })
            .ToList();

        return new
        {
            optionsetName = sanitizedOptionSetName,
            optionsetValues = optionsetValuesWithUniqueNames,
            @namespace = context.Namespace,
            version = context.Version,
        };
    }
}