namespace DataverseProxyGenerator.Core.Domain
{
    public record DoubleColumnModel : ColumnModel
    {
        public int? Precision { get; init; }
        public override string TypeName => "DoubleColumnModel";
    }
}
