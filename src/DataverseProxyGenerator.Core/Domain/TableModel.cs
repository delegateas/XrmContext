namespace DataverseProxyGenerator.Core.Domain;

public record TableModel
{
    public string LogicalName { get; init; } = string.Empty;

    public string SchemaName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public int EntityTypeCode { get; init; }

    public string PrimaryNameAttribute { get; init; } = string.Empty;

    public string PrimaryIdAttribute { get; init; } = string.Empty;

    public bool IsIntersect { get; init; }

    public IList<ColumnModel> Columns { get; init; } = [];

    public IList<RelationshipModel> Relationships { get; init; } = [];
}