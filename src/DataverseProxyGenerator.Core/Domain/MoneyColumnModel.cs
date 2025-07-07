namespace DataverseProxyGenerator.Core.Domain
{
    public record MoneyColumnModel : ColumnModel
    {
        public int? Precision { get; init; }
    }
}
