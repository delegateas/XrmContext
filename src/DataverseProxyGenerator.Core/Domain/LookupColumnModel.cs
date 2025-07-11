namespace DataverseProxyGenerator.Core.Domain;

public record LookupColumnModel : ColumnModel
{
    public string TargetTable { get; init; } = string.Empty;

    public string RelationshipName { get; init; } = string.Empty;
}
