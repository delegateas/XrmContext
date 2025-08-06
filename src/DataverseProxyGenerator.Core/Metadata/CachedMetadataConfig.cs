namespace DataverseProxyGenerator.Core.Metadata;

public record CachedMetadataConfig(XrmFetchConfig InnerConfig, TimeSpan CacheDuration);