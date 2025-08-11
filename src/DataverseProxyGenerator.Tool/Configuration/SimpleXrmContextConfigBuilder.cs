using DataverseProxyGenerator.Core;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Metadata;
using Microsoft.Extensions.Configuration;

namespace DataverseProxyGenerator.Tool.Configuration;

public static class SimpleXrmContextConfigBuilder
{
    public static XrmContextConfig BuildFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var configSection = configuration.GetSection("XrmContext");
        return new XrmContextConfig(
            new XrmFetchConfig(
                configSection.GetSection("Solutions").Get<string[]>() ?? Array.Empty<string>(),
                configSection.GetSection("Entities").Get<string[]>() ?? Array.Empty<string>(),
                configSection.GetValue<string>("DeprecatedPrefix") ?? string.Empty,
                configSection.GetSection("LabelMapping").Get<IReadOnlyDictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.InvariantCulture)),
            new XrmGenerationConfig(
                configSection.GetValue<string>("OutputDirectory") ?? string.Empty,
                configSection.GetValue<string>("NamespaceSetting") ?? "DataverseContext",
                configSection.GetValue<string>("ServiceContextName") ?? "Xrm",
                configSection.GetSection("IntersectMapping").Get<IReadOnlyDictionary<string, IReadOnlyList<string>>>() ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
                configSection.GetValue<bool>("GenerateCustomApis", true)));
    }
}