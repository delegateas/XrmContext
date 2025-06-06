namespace DataverseProxyGenerator.Core.Domain
{
    public record MoneyColumnModel : ColumnModel
    {
        public int? Precision { get; init; }
        public override string TypeName => "MoneyColumnModel";
    }
}
