namespace DataverseProxyGenerator.Core.Domain;

public record StringColumnModel : ColumnModel
{
    public int? MaxLength { get; init; }
}
