using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataverseProxyGenerator.Core.Domain;
using Scriban;

namespace DataverseProxyGenerator.Core.Generation
{
    public class CSharpProxyGenerator : ICodeGenerator
    {
        private static string GetTemplatesDirectory()
        {
            // Get the directory of the currently executing assembly (Core project)
            var assemblyDir = Path.GetDirectoryName(typeof(CSharpProxyGenerator).Assembly.Location);
            if (assemblyDir == null)
                throw new DirectoryNotFoundException("Could not determine the directory of the executing assembly.");
            // Traverse up to the project root (assume /bin/Debug/net8.0/ -> project root)
            var dir = new DirectoryInfo(assemblyDir);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            {
                dir = dir.Parent;
            }
            if (dir == null)
                throw new DirectoryNotFoundException("Could not locate project root for template resolution.");
            return Path.Combine(dir.FullName, "src", "DataverseProxyGenerator.Core", "Templates");
        }

        private static string ProxyClassTemplatePath => Path.Combine(GetTemplatesDirectory(), "ProxyClass.scriban-cs");
        private static string EnumTemplatePath => Path.Combine(GetTemplatesDirectory(), "EnumOptionset.scriban-cs");

        public CSharpProxyGenerator()
        {
            // No parameters needed, templates are always loaded from known location
        }

        public IEnumerable<GeneratedFile> GenerateCode(IEnumerable<TableModel> tables, string @namespace)
        {
            var (template, enumTemplate) = LoadTemplates();

            var files = new List<GeneratedFile>();

            files.AddRange(GenerateTableProxyFiles(tables, @namespace, template));
            files.AddRange(GenerateEnumFiles(GetGlobalOptionsets(tables), @namespace, enumTemplate));

            return files;
        }

        private (Template proxyTemplate, Template enumTemplate) LoadTemplates()
        {
            var templateText = File.ReadAllText(ProxyClassTemplatePath);
            var proxyTemplate = Template.Parse(templateText);

            var enumTemplateText = File.ReadAllText(EnumTemplatePath);
            var enumTemplate = Template.Parse(enumTemplateText);

            return (proxyTemplate, enumTemplate);
        }

        private IEnumerable<GeneratedFile> GenerateTableProxyFiles(IEnumerable<TableModel> tables, string @namespace, Template template)
        {
            foreach (var table in tables)
            {
                var result = template.Render(new { table, @namespace }, member => member.Name);
                yield return new GeneratedFile($"{table.SchemaName}.cs", result);
            }
        }

        private IEnumerable<EnumColumnModel> GetGlobalOptionsets(IEnumerable<TableModel> tables)
        {
            return tables
                .SelectMany(t => t.Columns)
                .OfType<EnumColumnModel>()
                .Where(c => c.IsGlobalOptionset && !string.IsNullOrEmpty(c.OptionsetName) && c.OptionsetValues != null)
                .GroupBy(c => c.OptionsetName)
                .Select(g => g.First());
        }

        private IEnumerable<GeneratedFile> GenerateEnumFiles(IEnumerable<EnumColumnModel> globalOptionsets, string @namespace, Template enumTemplate)
        {
            foreach (var optionset in globalOptionsets)
            {
                var enumResult = enumTemplate.Render(new
                {
                    optionsetName = optionset.OptionsetName,
                    optionsetValues = optionset.OptionsetValues.Select(kvp => new
                    {
                        Value = kvp.Key,
                        Name = kvp.Value
                    }),
                    @namespace
                }, member => member.Name);

                yield return new GeneratedFile(Path.Combine("optionsets", $"{optionset.OptionsetName}.cs"), enumResult);
            }
        }
    }
}
