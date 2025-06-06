namespace DataverseProxyGenerator.Core.Domain
{
    public record DecimalColumnModel : ColumnModel
    {
        public int? Precision { get; init; }
        public override string TypeName => "DecimalColumnModel";
    }
}
