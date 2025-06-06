namespace DataverseProxyGenerator.Core.Domain
{
    public record RelationshipModel
    {
        public string RelationshipName { get; init; }
        public string RelatedTable { get; init; }
        public string RelationshipType { get; init; }
    }
}
