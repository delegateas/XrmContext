namespace DataverseProxyGenerator.Core.Generation;

public interface IFileGenerator<in T>
{
    IAsyncEnumerable<GeneratedFile> GenerateAsync(T input, GenerationContext context);
}