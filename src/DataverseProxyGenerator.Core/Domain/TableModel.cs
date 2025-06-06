using System.Collections.Generic;

namespace DataverseProxyGenerator.Core.Domain
{
    public record TableModel
    {
        public string LogicalName { get; init; }
        public string SchemaName { get; init; }
        public string DisplayName { get; init; }
        public List<ColumnModel> Columns { get; init; } = new();
        public List<RelationshipModel> Relationships { get; init; } = new();
    }
}
