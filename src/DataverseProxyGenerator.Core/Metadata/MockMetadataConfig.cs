using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Metadata;

public record MockMetadataConfig(IEnumerable<TableModel> Tables);