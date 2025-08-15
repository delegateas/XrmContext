namespace DataverseProxyGenerator.Core.Generation;

public record XrmGenerationConfig(
    string OutputDirectory,
    string NamespaceSetting,
    string ServiceContextName,
    IReadOnlyDictionary<string, IReadOnlyList<string>> IntersectMapping,
    bool SingleFile = false,
    bool GenerateCustomApis = true);