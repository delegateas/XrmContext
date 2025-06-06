using System.Collections.Generic;
using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation
{
    /// <summary>
    /// Represents a generated code file (filename and content).
    /// </summary>
    public sealed record GeneratedFile(string Filename, string Content);

    public interface ICodeGenerator
    {
        /// <summary>
        /// Generates code files from the provided Dataverse table models.
        /// </summary>
        /// <param name="tables">The Dataverse table models to generate code for.</param>
        /// <param name="namespace">The namespace to use in generated code.</param>
        /// <returns>A collection of generated files (filename and content).</returns>
        IEnumerable<GeneratedFile> GenerateCode(IEnumerable<TableModel> tables, string @namespace);
    }
}
