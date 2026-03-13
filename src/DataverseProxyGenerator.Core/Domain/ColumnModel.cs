namespace DataverseProxyGenerator.Core.Domain;

public abstract record ColumnModel
{
    public required string LogicalName { get; init; }

    public required string SchemaName { get; init; }

    public required string DisplayName { get; init; }

    public string? Description { get; init; }

    public bool IsObsolete { get; init; }

    public string TypeName => GetType().Name;

    public string CSharpType { get; init; } = string.Empty;

    public string GetterSuffix { get; init; } = string.Empty;
}