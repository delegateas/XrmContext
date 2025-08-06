using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Metadata;

public class MockMetadataFetcher : IDataverseMetadataFetcher
{
    private readonly IEnumerable<TableModel> tables;

    public MockMetadataFetcher(IEnumerable<TableModel> tables)
    {
        this.tables = tables;
    }

    public Task<IEnumerable<TableModel>> FetchMetadataAsync()
    {
        return Task.FromResult(tables);
    }
}