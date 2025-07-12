using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Metadata;

namespace DataverseProxyGenerator.Core;

public record XrmContextConfig(
    XrmFetchConfig Fetch,
    XrmGenerationConfig Generation);