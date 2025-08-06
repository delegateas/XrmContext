namespace DataverseProxyGenerator.Core.Generation.Utilities;

public static class FilePathHelper
{
    /// <summary>
    /// Gets the output path for a table/entity proxy class file.
    /// </summary>
    /// <param name="schemaName">The schema name of the table.</param>
    /// <returns>The relative file path.</returns>
    public static string GetTableFilePath(string schemaName)
    {
        var sanitizedName = NameSanitizer.SanitizeName(schemaName, "UnknownTable");
        return Path.Combine("tables", $"{sanitizedName}.cs");
    }

    /// <summary>
    /// Gets the output path for an option set enum file.
    /// </summary>
    /// <param name="optionSetName">The name of the option set.</param>
    /// <returns>The relative file path.</returns>
    public static string GetOptionSetFilePath(string optionSetName)
    {
        var sanitizedName = NameSanitizer.SanitizeName(optionSetName, "UnknownOptionSet");
        return Path.Combine("optionsets", $"{sanitizedName}.cs");
    }

    /// <summary>
    /// Gets the output path for an intersection interface file.
    /// </summary>
    /// <param name="interfaceName">The name of the interface.</param>
    /// <returns>The relative file path.</returns>
    public static string GetIntersectionInterfaceFilePath(string interfaceName)
    {
        var sanitizedName = NameSanitizer.SanitizeName(interfaceName, "IUnknownInterface");
        return Path.Combine("intersections", $"{sanitizedName}.cs");
    }

    /// <summary>
    /// Gets the output path for the Xrm context class file.
    /// </summary>
    /// <returns>The relative file path.</returns>
    public static string GetXrmContextFilePath()
    {
        return Path.Combine("queries", "Xrm.cs");
    }

    /// <summary>
    /// Gets the output path for an attribute class file.
    /// </summary>
    /// <param name="attributeName">The name of the attribute class.</param>
    /// <returns>The relative file path.</returns>
    public static string GetAttributeFilePath(string attributeName)
    {
        var folder = attributeName switch
        {
            "TableAttributeHelpers" or "ExtendedEntity" => "tables",
            _ => "attributes",
        };

        return Path.Combine(folder, $"{attributeName}.cs");
    }
}