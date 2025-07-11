namespace DataverseProxyGenerator.Core.Domain;

public record IntegerColumnModel : ColumnModel
{
    public int Min { get; init; }

    public int Max { get; init; }
}