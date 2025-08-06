using DataverseProxyGenerator.Tool.Configuration;

namespace DataverseProxyGenerator.Tool.Options;

public class GeneratorOptionsValidator : IOptionsValidator<GeneratorOptions>
{
    public ValidationResult Validate(GeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            errors.Add("Output directory cannot be null or empty");
        }
        else if (!IsValidPath(options.OutputDirectory))
        {
            errors.Add("Output directory must be a valid path");
        }

        if (string.IsNullOrWhiteSpace(options.NamespaceSetting))
        {
            errors.Add("Namespace setting cannot be null or empty");
        }
        else if (!IsValidCSharpIdentifier(options.NamespaceSetting))
        {
            errors.Add("Namespace setting must be a valid C# namespace");
        }

        if (!string.IsNullOrWhiteSpace(options.ServiceContextName) &&
            !IsValidCSharpIdentifier(options.ServiceContextName))
        {
            errors.Add("Service context name must be a valid C# identifier");
        }

        if (options.Solutions.Count == 0 && options.Entities.Count == 0)
        {
            errors.Add("At least one solution or entity must be specified");
        }

        foreach (var kvp in options.IntersectMapping)
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

        if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
            return false;

        return identifier.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
    }
}