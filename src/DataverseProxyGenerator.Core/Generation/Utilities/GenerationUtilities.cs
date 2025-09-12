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

    /// <summary>
    /// Extracts unique global optionsets from a collection of tables.
    /// </summary>
    /// <param name="tables">The collection of tables to extract optionsets from.</param>
    /// <returns>A collection of unique global optionsets.</returns>
    public static IEnumerable<EnumColumnModel> GetGlobalOptionsets(IEnumerable<TableModel> tables)
    {
        return tables
            .SelectMany(t => t.Columns)
            .OfType<EnumColumnModel>()
            .Where(c => !string.IsNullOrEmpty(c.OptionsetName) && c.OptionsetValues != null)
            .GroupBy(c => c.OptionsetName, StringComparer.InvariantCulture)
            .Select(g => g.First());
    }
}