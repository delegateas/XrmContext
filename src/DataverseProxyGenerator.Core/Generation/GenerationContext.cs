using DataverseProxyGenerator.Core.Templates;

namespace DataverseProxyGenerator.Core.Generation;

public record GenerationContext
{
    public required string Namespace { get; init; }

    public required string Version { get; init; }

    public required ITemplateProvider Templates { get; init; }

    public string? ServiceContextName { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> IntersectMapping { get; init; } = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
}