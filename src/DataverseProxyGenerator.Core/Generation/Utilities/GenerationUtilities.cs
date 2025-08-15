using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation.Utilities;

public static class GenerationUtilities
{
    /// <summary>
    /// Sanitizes a name to make it a valid C# identifier.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <param name="fallbackPrefix">Optional fallback prefix.</param>
    /// <returns>A sanitized name.</returns>
    public static string SanitizeName(string name, string fallbackPrefix = "Item")
    {
        return NameSanitizer.SanitizeName(name, fallbackPrefix);
    }

    /// <summary>
    /// Gets the type signature for a column using the shared utility.
    /// </summary>
    /// <param name="column">The column model.</param>
    /// <returns>The type signature.</returns>
    public static string GetTypeSignature(ColumnModel column)
    {
        return TypeSignatureHelper.GetPropertyTypeSignature(column);
    }
}
