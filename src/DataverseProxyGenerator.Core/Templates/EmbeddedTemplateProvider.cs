using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.Reflection;

namespace DataverseProxyGenerator.Core.Templates;

public class EmbeddedTemplateProvider : ITemplateLoader
{
    private readonly Dictionary<string, Template> templateCache = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Assembly assembly;

    public EmbeddedTemplateProvider()
    {
        assembly = typeof(EmbeddedTemplateProvider).Assembly;
    }

    public Template GetTemplate(string templateName)
    {
        if (templateCache.TryGetValue(templateName, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        try
        {
            var templateContent = GetEmbeddedResourceText(templateName);
            var template = Template.Parse(templateContent);

            if (template.HasErrors)
            {
                var errors = string.Join(Environment.NewLine, template.Messages.Select(m => m.ToString()));
                throw new InvalidOperationException($"Template parsing errors in '{templateName}':{Environment.NewLine}{errors}");
            }

            templateCache[templateName] = template;
            return template;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
        }
    }

    public bool HasTemplate(string templateName)
    {
        ArgumentNullException.ThrowIfNull(templateName);

        if (templateCache.ContainsKey(templateName))
        {
            return true;
        }

        // Normalize the template name for embedded resource lookup
        var normalizedName = templateName.Replace('/', '.').Replace('\\', '.');
        var fullResourceName = $"DataverseProxyGenerator.Core.Templates.{normalizedName}";
        return assembly.GetManifestResourceNames().Contains(fullResourceName, StringComparer.Ordinal);
    }

    private string GetEmbeddedResourceText(string resourceName)
    {
        var fullResourceName = $"DataverseProxyGenerator.Core.Templates.{resourceName}";

        // Debug: List all available resources if the requested one is not found
        var availableResources = assembly.GetManifestResourceNames();
        if (!availableResources.Contains(fullResourceName, StringComparer.Ordinal))
        {
            var resourceList = string.Join(Environment.NewLine, availableResources.Where(r => r.Contains("Templates", StringComparison.Ordinal)));
            throw new FileNotFoundException($"Could not find embedded resource: {fullResourceName}{Environment.NewLine}Available template resources:{Environment.NewLine}{resourceList}");
        }

        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
            throw new FileNotFoundException($"Could not find embedded resource: {fullResourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ITemplateLoader implementation
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        ArgumentNullException.ThrowIfNull(templateName);

        // Normalize path - convert slashes to dots for embedded resource naming
        return templateName.Replace('/', '.').Replace('\\', '.');
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        ArgumentNullException.ThrowIfNull(templatePath);

        try
        {
            // The templatePath is already normalized by GetPath, so use it directly
            return GetEmbeddedResourceText(templatePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load included template '{templatePath}': {ex.Message}", ex);
        }
    }

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        // For embedded resources, we can return the sync result wrapped in ValueTask
        var result = Load(context, callerSpan, templatePath);
        return new ValueTask<string>(result);
    }
}