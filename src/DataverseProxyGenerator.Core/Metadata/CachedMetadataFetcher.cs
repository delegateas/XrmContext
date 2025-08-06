using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Metadata;

public class CachedMetadataFetcher : IDataverseMetadataFetcher
{
    private readonly IDataverseMetadataFetcher innerFetcher;
    private readonly TimeSpan cacheDuration;
    private readonly Dictionary<string, (IEnumerable<TableModel> Tables, DateTime CachedAt)> cache = new(StringComparer.Ordinal);

    public CachedMetadataFetcher(IDataverseMetadataFetcher innerFetcher, TimeSpan cacheDuration)
    {
        this.innerFetcher = innerFetcher;
        this.cacheDuration = cacheDuration;
    }

    public async Task<IEnumerable<TableModel>> FetchMetadataAsync()
    {
        var cacheKey = GetCacheKey();

        if (cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < cacheDuration)
            {
                return cached.Tables;
            }

            cache.Remove(cacheKey);
        }

        var tables = await innerFetcher.FetchMetadataAsync();
        cache[cacheKey] = (tables, DateTime.UtcNow);

        return tables;
    }

    private string GetCacheKey()
    {
        // Simple cache key based on inner fetcher type
        // In a real implementation, this would include configuration details
        return innerFetcher.GetType().FullName ?? "unknown";
    }
}