namespace DataverseProxyGenerator.Core.Domain;

public record DecimalColumnModel : ColumnModel
{
    public int? Precision { get; init; }
}
