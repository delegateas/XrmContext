using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation;

public interface ICodeGenerator
{
    /// <summary>
    /// Generates code files from the provided Dataverse table models, including intersection interfaces.
    /// </summary>
    /// <param name="tables">The Dataverse table models to generate code for.</param>
    /// <param name="config">Configuration for code generation, including output directory, namespace, service context name, and intersection mappings.</param>
    /// <returns>A collection of generated files (filename and content).</returns>
    IEnumerable<GeneratedFile> GenerateCode(IEnumerable<TableModel> tables, XrmGenerationConfig config);

    /// <summary>
    /// Generates code files from the provided custom API models.
    /// </summary>
    /// <param name="customApis">The custom API models to generate code for.</param>
    /// <param name="config">Configuration for code generation, including namespace and version.</param>
    /// <returns>A collection of generated files (filename and content).</returns>
    IEnumerable<GeneratedFile> GenerateCustomApiCode(IEnumerable<CustomApiModel> customApis, XrmGenerationConfig config);
}