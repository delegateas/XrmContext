using System.Collections.ObjectModel;
using DataverseProxyGenerator.Core;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Metadata;

namespace DataverseProxyGenerator.Tests;

public class XrmContextConfigTests
{
    [Fact]
    public void CommandLineArgs_Override_Config_Values()
    {
        var config = new XrmContextConfig(
            new XrmFetchConfig(
                new ReadOnlyCollection<string>(["ConfigSolution"]),
                new ReadOnlyCollection<string>(["ConfigEntity"]),
                "ConfigPrefix",
                new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(StringComparer.InvariantCulture)
                    {
                        { "ConfigLabel", "ConfigValue" },
                    })),
            new XrmGenerationConfig(
                "fromConfig",
                "ConfigNamespace",
                "ConfigServiceContext",
                new ReadOnlyDictionary<string, IReadOnlyList<string>>(
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture)
                    {
                        { "ConfigKey", new ReadOnlyCollection<string>(["ConfigVal"]) },
                    })));

        // Simulate command-line args (some provided, some not)
        string outputDirectory = "fromArgs";
        IReadOnlyList<string> solutions = new ReadOnlyCollection<string>(["ArgSolution"]);
        IReadOnlyList<string> entities = [];
        string namespaceSetting = string.Empty;
        string serviceContextName = "ArgServiceContext";
        string deprecatedPrefix = string.Empty;
        var intersectMapping = new ReadOnlyDictionary<string, IReadOnlyList<string>>(new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture));
        var labelMapping = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.InvariantCulture) { { "ArgLabel", "ArgValue" } });

        var merged = new XrmContextConfig(
            new XrmFetchConfig(
                solutions.Count > 0 ? solutions : config.Fetch.Solutions,
                entities.Count > 0 ? entities : config.Fetch.Entities,
                !string.IsNullOrWhiteSpace(deprecatedPrefix) ? deprecatedPrefix : config.Fetch.DeprecatedPrefix,
                labelMapping.Count > 0 ? labelMapping : config.Fetch.LabelMappings),
            new XrmGenerationConfig(
                !string.IsNullOrWhiteSpace(outputDirectory) ? outputDirectory : config.Generation.OutputDirectory,
                !string.IsNullOrWhiteSpace(namespaceSetting) ? namespaceSetting : config.Generation.NamespaceSetting,
                !string.IsNullOrWhiteSpace(serviceContextName) ? serviceContextName : config.Generation.ServiceContextName,
                intersectMapping.Count > 0 ? intersectMapping : config.Generation.IntersectMapping));

        Assert.Equal("fromArgs", merged.Generation.OutputDirectory);
        Assert.Equal(["ArgSolution"], merged.Fetch.Solutions);
        Assert.Equal(["ConfigEntity"], merged.Fetch.Entities);
        Assert.Equal("ConfigNamespace", merged.Generation.NamespaceSetting);
        Assert.Equal("ArgServiceContext", merged.Generation.ServiceContextName);
        Assert.Equal("ConfigPrefix", merged.Fetch.DeprecatedPrefix);
        Assert.Equal(
            new ReadOnlyDictionary<string, IReadOnlyList<string>>(new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture) { { "ConfigKey", new ReadOnlyCollection<string>(["ConfigVal"]) } }),
            merged.Generation.IntersectMapping);
        Assert.Equal(
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.InvariantCulture) { { "ArgLabel", "ArgValue" } }),
            merged.Fetch.LabelMappings);
    }
}