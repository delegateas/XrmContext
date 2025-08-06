namespace DataverseProxyGenerator.Core.Generation;

public interface IFileGenerator<in T>
{
    IEnumerable<GeneratedFile> Generate(T input, GenerationContext context);
}