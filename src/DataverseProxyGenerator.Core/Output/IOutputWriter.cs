using System.Collections.Generic;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Core.Output
{
    public interface IOutputWriter
    {
        /// <summary>
        /// Writes the generated files to the specified output directory.
        /// </summary>
        /// <param name="files">The generated files to write.</param>
        /// <param name="outputDirectory">The output directory path.</param>
        void WriteFiles(IEnumerable<GeneratedFile> files, string outputDirectory);
    }
}
