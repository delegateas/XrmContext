using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Core.Output;

public interface IOutputWriter
{
    /// <summary>
    /// Writes the generated files to the specified output directory.
    /// </summary>
    /// <param name="files">The generated files to write.</param>
    /// <param name="outputDirectory">The output directory path.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task WriteFilesAsync(IAsyncEnumerable<GeneratedFile> files, string outputDirectory);
}