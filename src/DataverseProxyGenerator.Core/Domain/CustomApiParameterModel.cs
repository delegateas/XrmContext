namespace DataverseProxyGenerator.Core.Domain;

public record CustomApiParameterModel
{
    public string Name { get; init; } = string.Empty;

    public string UniqueName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public CustomApiParameterType Type { get; init; }

    public bool IsOptional { get; init; }

    public string? LogicalEntityName { get; init; }
}