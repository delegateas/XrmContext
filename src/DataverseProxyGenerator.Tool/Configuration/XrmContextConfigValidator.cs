using DataverseProxyGenerator.Core;

namespace DataverseProxyGenerator.Tool.Configuration;

public class XrmContextConfigValidator : IOptionsValidator<XrmContextConfig>
{
    public ValidationResult Validate(XrmContextConfig options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        // Validate generation config
        if (string.IsNullOrWhiteSpace(options.Generation.OutputDirectory))
        {
            errors.Add("OutputDirectory cannot be null or empty");
        }
        else if (!IsValidPath(options.Generation.OutputDirectory))
        {
            errors.Add("OutputDirectory must be a valid path");
        }

        if (string.IsNullOrWhiteSpace(options.Generation.NamespaceSetting))
        {
            errors.Add("NamespaceSetting cannot be null or empty");
        }
        else if (!IsValidCSharpIdentifier(options.Generation.NamespaceSetting))
        {
            errors.Add("NamespaceSetting must be a valid C# namespace");
        }

        if (!string.IsNullOrWhiteSpace(options.Generation.ServiceContextName) &&
            !IsValidCSharpIdentifier(options.Generation.ServiceContextName))
        {
            errors.Add("ServiceContextName must be a valid C# identifier");
        }

        // Validate fetch config
        if (options.Fetch.Solutions.Count == 0 && options.Fetch.Entities.Count == 0)
        {
            errors.Add("At least one solution or entity must be specified");
        }

        // Validate intersection mappings
        foreach (var kvp in options.Generation.IntersectMapping)
        {
            if (!IsValidCSharpIdentifier(kvp.Key))
            {
                errors.Add($"Interface name '{kvp.Key}' is not a valid C# identifier");
            }

            if (kvp.Value.Count < 2)
            {
                errors.Add($"Interface '{kvp.Key}' must have at least 2 tables to intersect");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool IsValidCSharpIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        // Simple validation - starts with letter or underscore, contains only letters, digits, underscores, dots
        if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
            return false;

        return identifier.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
    }
}