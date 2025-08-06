using DataverseProxyGenerator.Core.Generation.Utilities;
using Scriban;

namespace DataverseProxyGenerator.Core.Generation.Common;

/// <summary>
/// Base class for file generators providing common functionality.
/// </summary>
public abstract class BaseFileGenerator
{
    /// <summary>
    /// Validates the generation context.
    /// </summary>
    /// <param name="context">The generation context to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    protected static void ValidateContext(GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Templates);

        if (string.IsNullOrWhiteSpace(context.Namespace))
        {
            throw new ArgumentException("Namespace cannot be null or empty", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.Version))
        {
            throw new ArgumentException("Version cannot be null or empty", nameof(context));
        }
    }

    /// <summary>
    /// Creates a template context with standard settings.
    /// </summary>
    /// <param name="model">The model to use for rendering.</param>
    /// <returns>A configured template context.</returns>
    protected static TemplateContext CreateTemplateContext(object model)
    {
        var templateContext = new TemplateContext(StringComparer.InvariantCulture)
        {
            LoopLimit = 0, // No limit
            MemberRenamer = member => member.Name,
        };
        templateContext.PushGlobal(Scriban.Runtime.ScriptObject.From(model));
        return templateContext;
    }

    /// <summary>
    /// Sanitizes a name using the shared utility.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <param name="fallbackPrefix">Optional fallback prefix.</param>
    /// <returns>A sanitized name.</returns>
    protected static string SanitizeName(string name, string fallbackPrefix = "Item")
    {
        return NameSanitizer.SanitizeName(name, fallbackPrefix);
    }

    /// <summary>
    /// Gets the type signature for a column using the shared utility.
    /// </summary>
    /// <param name="column">The column model.</param>
    /// <returns>The type signature.</returns>
    protected static string GetTypeSignature(Domain.ColumnModel column)
    {
        return TypeSignatureHelper.GetPropertyTypeSignature(column);
    }
}