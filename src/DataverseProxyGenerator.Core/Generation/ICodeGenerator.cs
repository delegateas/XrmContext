using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation;

public interface ICodeGenerator
{
    /// <summary>
    /// Generates code files from the provided Dataverse table models, including intersection interfaces.
    /// </summary>
    /// <param name="tables">The Dataverse table models to generate code for.</param>
    /// <param name="namespaceSetting">The namespace to use in generated code.</param>
    /// <param name="serviceContextName"> The name of the service context class to generate.</param>
    /// <param name="intersectMapping">Mapping of interface names to lists of table schema names for intersection interfaces.</param>
    /// <returns>A collection of generated files (filename and content).</returns>
    IEnumerable<GeneratedFile> GenerateCode(IEnumerable<TableModel> tables, string namespaceSetting, string serviceContextName, IDictionary<string, List<string>> intersectMapping);
}