namespace DataverseProxyGenerator.Core.Exceptions;

public class MetadataFetchException : Exception
{
    public string EntityName { get; }

    public string? SolutionName { get; }

    public MetadataFetchException()
    {
        EntityName = string.Empty;
    }

    public MetadataFetchException(string message)
        : base(message)
    {
        EntityName = string.Empty;
    }

    public MetadataFetchException(string message, Exception innerException)
        : base(message, innerException)
    {
        EntityName = string.Empty;
    }

    public MetadataFetchException(string message, string entityName)
        : base(message)
    {
        EntityName = entityName;
    }

    public MetadataFetchException(string message, string entityName, string? solutionName)
        : base(message)
    {
        EntityName = entityName;
        SolutionName = solutionName;
    }

    public MetadataFetchException(string message, string entityName, Exception innerException)
        : base(message, innerException)
    {
        EntityName = entityName;
    }

    public MetadataFetchException(string message, string entityName, string? solutionName, Exception innerException)
        : base(message, innerException)
    {
        EntityName = entityName;
        SolutionName = solutionName;
    }
}