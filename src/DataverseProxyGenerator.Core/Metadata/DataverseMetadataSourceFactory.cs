using Microsoft.PowerPlatform.Dataverse.Client;

namespace DataverseProxyGenerator.Core.Metadata;

public class DataverseMetadataSourceFactory : IMetadataSourceFactory
{
    private readonly ServiceClient serviceClient;

    public DataverseMetadataSourceFactory(ServiceClient serviceClient)
    {
        this.serviceClient = serviceClient;
    }

    public IDataverseMetadataFetcher CreateFetcher(MetadataSourceType type, object config)
    {
        return type switch
        {
            MetadataSourceType.Dataverse => CreateDataverseFetcher(config),
            MetadataSourceType.Cached => CreateCachedFetcher(config),
            MetadataSourceType.Mock => CreateMockFetcher(config),
            _ => throw new NotSupportedException($"Metadata source type {type} is not supported by this factory"),
        };
    }

    public bool SupportsSourceType(MetadataSourceType type)
    {
        return type is MetadataSourceType.Dataverse or MetadataSourceType.Cached or MetadataSourceType.Mock;
    }

    private IDataverseMetadataFetcher CreateDataverseFetcher(object config)
    {
        if (config is not XrmFetchConfig fetchConfig)
            throw new ArgumentException("Expected XrmFetchConfig for Dataverse metadata source", nameof(config));

        return new DataverseMetadataFetcher(serviceClient, fetchConfig);
    }

    private IDataverseMetadataFetcher CreateCachedFetcher(object config)
    {
        if (config is not CachedMetadataConfig cachedConfig)
            throw new ArgumentException("Expected CachedMetadataConfig for Cached metadata source", nameof(config));

        var innerFetcher = CreateDataverseFetcher(cachedConfig.InnerConfig);
        return new CachedMetadataFetcher(innerFetcher, cachedConfig.CacheDuration);
    }

    private static IDataverseMetadataFetcher CreateMockFetcher(object config)
    {
        if (config is not MockMetadataConfig mockConfig)
            throw new ArgumentException("Expected MockMetadataConfig for Mock metadata source", nameof(config));

        return new MockMetadataFetcher(mockConfig.Tables);
    }
}