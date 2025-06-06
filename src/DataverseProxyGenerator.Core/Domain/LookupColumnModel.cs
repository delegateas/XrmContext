namespace DataverseProxyGenerator.Core.Domain
{
    public record LookupColumnModel : ColumnModel
    {
        public string TargetTable { get; init; }
        public string RelationshipName { get; init; }
        public override string TypeName => "LookupColumnModel";
    }
}
