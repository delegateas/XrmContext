namespace DataverseProxyGenerator.Core.Domain;

public record AlternateKeyModel
{
    public string SchemaName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public IList<ColumnModel> KeyAttributes { get; init; } = [];
}