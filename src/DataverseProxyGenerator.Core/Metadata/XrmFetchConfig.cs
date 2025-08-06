namespace DataverseProxyGenerator.Core.Metadata;

public record XrmFetchConfig(
    IReadOnlyList<string> Solutions,
    IReadOnlyList<string> Entities,
    string DeprecatedPrefix,
    IReadOnlyDictionary<string, string> LabelMapping);