using System.Collections.Generic;
using System.Threading.Tasks;
using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Metadata
{
    public interface IDataverseMetadataFetcher
    {
        /// <summary>
        /// Fetches metadata for all Dataverse tables within the specified solutions and/or by logical names, including columns and relationships.
        /// </summary>
        /// <param name="serviceClient">The ServiceClient for Dataverse authentication.</param>
        /// <param name="solutionUniqueNames">A collection of solution unique names to filter entities by solution. If null or empty, fetches all entities.</param>
        /// <param name="logicalNames">A collection of entity logical names to fetch. Only logical names not already fetched from solutions will be fetched. Can be null or empty.</param>
        /// <param name="deprecatedPrefix">A prefix to mark attributes as deprecated if their display name starts with it. If null or empty, no attributes are marked as deprecated.</param>
        /// <returns>A list of TableModel objects representing the Dataverse schema.</returns>
        Task<List<TableModel>> FetchMetadataAsync(
            Microsoft.PowerPlatform.Dataverse.Client.ServiceClient serviceClient,
            IEnumerable<string> solutionUniqueNames,
            IEnumerable<string> logicalNames,
            string? deprecatedPrefix);
    }
}
