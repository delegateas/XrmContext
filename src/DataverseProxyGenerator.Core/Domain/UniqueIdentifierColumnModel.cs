namespace DataverseProxyGenerator.Core.Domain
{
    public record UniqueIdentifierColumnModel : ColumnModel
    {
        public bool IsUniqueIdentifier { get; init; }
        public override string TypeName => "UniqueIdentifierColumnModel";
    }
}
