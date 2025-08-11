namespace DataverseProxyGenerator.Core.Generation.Utilities;

public static class FilePathHelper
{
    /// <summary>
    /// Gets the output path for a table/entity proxy class file.
    /// </summary>
    /// <param name="sanitizedName">The sanitized schema name of the table.</param>
    /// <returns>The relative file path.</returns>
    public static string GetTableFilePath(string sanitizedName)
    {
        return Path.Combine("tables", $"{sanitizedName}.cs");
    }

    /// <summary>
    /// Gets the output path for an option set enum file.
    /// </summary>
    /// <param name="sanitizedName">The sanitized name of the option set.</param>
    /// <returns>The relative file path.</returns>
    public static string GetOptionSetFilePath(string sanitizedName)
    {
        return Path.Combine("optionsets", $"{sanitizedName}.cs");
    }

    /// <summary>
    /// Gets the output path for an intersection interface file.
    /// </summary>
    /// <param name="sanitizedName">The sanitized name of the interface.</param>
    /// <returns>The relative file path.</returns>
    public static string GetIntersectionInterfaceFilePath(string sanitizedName)
    {
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
    /// Gets the output path for a helper file.
    /// </summary>
    /// <param name="fileName">The name of the helper file.</param>
    /// <returns>The relative file path.</returns>
    public static string GetHelperFilePath(string fileName)
    {
        var folder = fileName switch
        {
            "TableAttributeHelpers" or "ExtendedEntity" => "tables",
            _ => "attributes",
        };

        return Path.Combine(folder, $"{fileName}.cs");
    }

    /// <summary>
    /// Gets the output path for custom API files.
    /// </summary>
    public static string CustomApiPath => "customapis";
}