using Scriban;
using System.Reflection;

namespace DataverseProxyGenerator.Core.Templates;

public class EmbeddedTemplateProvider
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

        var templateContent = GetEmbeddedResourceText(templateName);
        var template = Template.Parse(templateContent);
        templateCache[templateName] = template;

        return template;
    }

    public bool HasTemplate(string templateName)
    {
        if (templateCache.ContainsKey(templateName))
        {
            return true;
        }

        var fullResourceName = $"DataverseProxyGenerator.Core.Templates.{templateName}";
        return assembly.GetManifestResourceNames().Contains(fullResourceName, StringComparer.Ordinal);
    }

    private string GetEmbeddedResourceText(string resourceName)
    {
        var fullResourceName = $"DataverseProxyGenerator.Core.Templates.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
            throw new FileNotFoundException($"Could not find embedded resource: {fullResourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}