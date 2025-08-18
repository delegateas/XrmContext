namespace DataverseProxyGenerator.Core.Generation.Utilities;

public static class NameSanitizer
{
    private static readonly char[] DisallowedCharacters = { '(', ')', '\'', '-', '–', '%', ' ', '.', ',', ':', ';', '/', '\\', '+', '\n', '\r', '\t', '&' };
    private static readonly string[] ReservedKeywords =
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
        "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    };

    /// <summary>
    /// Sanitizes a name to make it a valid C# identifier.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <param name="fallbackPrefix">Prefix to use if name is empty or invalid. Defaults to "Item".</param>
    /// <returns>A valid C# identifier.</returns>
    public static string SanitizeName(string name, string fallbackPrefix = "Item")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return GenerateFallbackName(fallbackPrefix);
        }

        // Remove disallowed characters entirely
        var cleaned = string.Concat(name.Where(c => !DisallowedCharacters.Contains(c)));

        // Ensure it doesn't start with a digit
        if (cleaned.Length > 0 && char.IsDigit(cleaned[0]))
        {
            cleaned = "_" + cleaned;
        }

        // Ensure it starts with a letter or underscore
        if (cleaned.Length == 0 || (!char.IsLetter(cleaned[0]) && cleaned[0] != '_'))
        {
            cleaned = "_" + cleaned;
        }

        // Handle reserved keywords
        if (IsReservedKeyword(cleaned))
        {
            cleaned = "@" + cleaned;
        }

        // Final fallback if still empty or invalid
        return string.IsNullOrWhiteSpace(cleaned) ? GenerateFallbackName(fallbackPrefix) : cleaned;
    }

    /// <summary>
    /// Sanitizes an enum option label to make it a valid C# enum member name.
    /// </summary>
    /// <param name="label">The option label to sanitize.</param>
    /// <param name="optionValue">The option value to use as fallback.</param>
    /// <returns>A valid C# enum member name.</returns>
    public static string SanitizeEnumOptionName(string label, int optionValue)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return $"Option_{optionValue}";
        }

        return SanitizeName(label, $"Option_{optionValue}");
    }

    /// <summary>
    /// Sanitizes a string to make it a valid single line string.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>A single line string.</returns>
    public static string SanitizeString(string input)
    {
        input ??= string.Empty;
        return input
            .Replace("\r", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a string is a C# reserved keyword.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is a reserved keyword.</returns>
    public static bool IsReservedKeyword(string name)
    {
        return ReservedKeywords.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates a fallback name when the original name is unusable.
    /// </summary>
    /// <param name="prefix">The prefix to use.</param>
    /// <returns>A unique fallback name.</returns>
    private static string GenerateFallbackName(string prefix)
    {
#pragma warning disable SCS0005 // Weak random number generator.
#pragma warning disable CA5394 // Do not use insecure randomness
        var rand = new Random();
        return prefix + rand.Next(1000, 10000);
#pragma warning restore CA5394 // Do not use insecure randomness
#pragma warning restore SCS0005 // Weak random number generator.
    }
}