using System.Collections.Generic;
using System.IO;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Core.Output
{
    public class FileSystemOutputWriter : IOutputWriter
    {
        public void WriteFiles(IEnumerable<GeneratedFile> files, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            foreach (var file in files)
            {
                var filePath = Path.Combine(outputDirectory, file.Filename);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, file.Content);
            }
        }
    }
}
