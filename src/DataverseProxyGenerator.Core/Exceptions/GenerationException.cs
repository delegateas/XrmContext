namespace DataverseProxyGenerator.Core.Exceptions;

public class GenerationException : Exception
{
    public string Context { get; }

    public object? Details { get; }

    public GenerationException()
    {
        Context = string.Empty;
    }

    public GenerationException(string message)
        : base(message)
    {
        Context = string.Empty;
    }

    public GenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Context = string.Empty;
    }

    public GenerationException(string message, string context)
        : base(message)
    {
        Context = context;
    }

    public GenerationException(string message, string context, object? details)
        : base(message)
    {
        Context = context;
        Details = details;
    }

    public GenerationException(string message, string context, Exception innerException)
        : base(message, innerException)
    {
        Context = context;
    }

    public GenerationException(string message, string context, object? details, Exception innerException)
        : base(message, innerException)
    {
        Context = context;
        Details = details;
    }
}