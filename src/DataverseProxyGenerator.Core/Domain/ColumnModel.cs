namespace DataverseProxyGenerator.Core.Domain
{
    public abstract record ColumnModel
    {
        public required string LogicalName { get; init; }
        public required string SchemaName { get; init; }
        public required string DisplayName { get; init; }
        public string? Description { get; init; }
        public bool IsNullable { get; init; }
        public bool IsObsolete { get; init; }
        public string TypeName => GetType().Name;
    }
}
