using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Metadata;

public interface IDataverseMetadataFetcher
{
    /// <summary>
    /// Fetches metadata for all Dataverse tables within the specified solutions and/or by logical names, including columns and relationships.
    /// </summary>
    /// <returns>A list of TableModel objects representing the Dataverse schema.</returns>
    Task<IEnumerable<TableModel>> FetchMetadataAsync();
}