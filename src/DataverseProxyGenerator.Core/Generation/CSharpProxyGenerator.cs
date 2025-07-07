using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataverseProxyGenerator.Core.Domain;
using Scriban;

namespace DataverseProxyGenerator.Core.Generation
{
    public class CSharpProxyGenerator : ICodeGenerator
    {
        // Helper struct for fast column comparison
        private struct ColumnSignature
        {
            public string SchemaName { get; }
            public string TypeName { get; }
            public ColumnSignature(string schemaName, string typeName)
            {
                SchemaName = schemaName;
                TypeName = typeName;
            }
            public override bool Equals(object obj)
            {
                return obj is ColumnSignature other &&
                    SchemaName == other.SchemaName &&
                    TypeName == other.TypeName;
            }
            public override int GetHashCode()
            {
                return (SchemaName, TypeName).GetHashCode();
            }
        }

        private static string GetTemplatesDirectory()
        {
            var assemblyDir = Path.GetDirectoryName(typeof(CSharpProxyGenerator).Assembly.Location);
            if (assemblyDir == null)
                throw new DirectoryNotFoundException("Could not determine the directory of the executing assembly.");
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
        private static string IntersectionInterfaceTemplatePath => Path.Combine(GetTemplatesDirectory(), "IntersectionInterface.scriban-cs");
        private static string OptionSetMetadataAttributeTemplatePath => Path.Combine(GetTemplatesDirectory(), "OptionSetMetadataAttribute.scriban-cs");
        private static string XrmClassTemplatePath => Path.Combine(GetTemplatesDirectory(), "XrmClass.scriban-cs");

        public CSharpProxyGenerator()
        {
        }

        public IEnumerable<GeneratedFile> GenerateCode(IEnumerable<TableModel> tables, string @namespace, Dictionary<string, List<string>> intersectMapping)
        {
            var (proxyTemplate, enumTemplate, interfaceTemplate, optionSetMetadataAttributeTemplate) = LoadAllTemplatesWithAttribute();

            var files = new List<GeneratedFile>();

            var tableDict = tables.ToDictionary(t => t.LogicalName, t => t);
            var tableColumns = BuildTableColumns(tables);

            var (interfaceColumns, tableToInterfaces) = BuildIntersectionData(intersectMapping, tableDict, tableColumns);

            files.AddRange(GenerateIntersectionInterfaceFiles(interfaceColumns, tables, @namespace, interfaceTemplate));

            // Generate proxy classes (with interfaces if needed)
            foreach (var table in tables)
            {
                var interfaces = tableToInterfaces.TryGetValue(table.LogicalName, out var ifaces) ? ifaces : new List<string>();
                var model = new
                {
                    table = new
                    {
                        SchemaName = table.SchemaName,
                        Columns = table.Columns,
                        Relationships = table.Relationships,
                        LogicalName = table.LogicalName,
                        DisplayName = table.DisplayName,
                        EntityTypeCode = table.EntityTypeCode,
                        PrimaryNameAttribute = table.PrimaryNameAttribute,
                        PrimaryIdAttribute = table.PrimaryIdAttribute,
                        InterfacesList = interfaces ?? new List<string>()
                    },
                    @namespace
                };
                var context = new Scriban.TemplateContext();
                context.LoopLimit = 0; // 0 means no limit
                context.MemberRenamer = member => member.Name;
                context.PushGlobal(Scriban.Runtime.ScriptObject.From(model));
                var result = proxyTemplate.Render(context);
                files.Add(new GeneratedFile(Path.Combine("tables", $"{table.SchemaName}.cs"), result));
            }

            // Generate enums as before
            files.AddRange(GenerateEnumFiles(GetGlobalOptionsets(tables), @namespace, enumTemplate));

            // Generate Xrm context class
            var xrmClassTemplateText = File.ReadAllText(XrmClassTemplatePath);
            var xrmClassTemplate = Template.Parse(xrmClassTemplateText);
            var xrmClassResult = xrmClassTemplate.Render(new { tables }, member => member.Name);
            files.Add(new GeneratedFile(Path.Combine("queries", "Xrm.cs"), xrmClassResult));

            // Generate OptionSetMetadataAttribute
            var attributeResult = optionSetMetadataAttributeTemplate.Render(new { @namespace }, member => member.Name);
            files.Add(new GeneratedFile(Path.Combine("attributes", "OptionSetMetadataAttribute.cs"), attributeResult));

            return files;
        }

        private (Template proxyTemplate, Template enumTemplate, Template interfaceTemplate) LoadAllTemplates()
        {
            var proxyTemplateText = File.ReadAllText(ProxyClassTemplatePath);
            var proxyTemplate = Template.Parse(proxyTemplateText);

            var enumTemplateText = File.ReadAllText(EnumTemplatePath);
            var enumTemplate = Template.Parse(enumTemplateText);

            var interfaceTemplateText = File.ReadAllText(IntersectionInterfaceTemplatePath);
            var interfaceTemplate = Template.Parse(interfaceTemplateText);

            return (proxyTemplate, enumTemplate, interfaceTemplate);
        }

        private (Template proxyTemplate, Template enumTemplate, Template interfaceTemplate, Template optionSetMetadataAttributeTemplate) LoadAllTemplatesWithAttribute()
        {
            var proxyTemplateText = File.ReadAllText(ProxyClassTemplatePath);
            var proxyTemplate = Template.Parse(proxyTemplateText);

            var enumTemplateText = File.ReadAllText(EnumTemplatePath);
            var enumTemplate = Template.Parse(enumTemplateText);

            var interfaceTemplateText = File.ReadAllText(IntersectionInterfaceTemplatePath);
            var interfaceTemplate = Template.Parse(interfaceTemplateText);

            var optionSetMetadataAttributeTemplateText = File.ReadAllText(OptionSetMetadataAttributeTemplatePath);
            var optionSetMetadataAttributeTemplate = Template.Parse(optionSetMetadataAttributeTemplateText);

            return (proxyTemplate, enumTemplate, interfaceTemplate, optionSetMetadataAttributeTemplate);
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
                        Name = kvp.Value,
                        Localizations = optionset.OptionLocalizations != null && optionset.OptionLocalizations.ContainsKey(kvp.Key)
                            ? optionset.OptionLocalizations[kvp.Key]
                            : new Dictionary<int, string>()
                    }),
                    @namespace
                }, member => member.Name);

                yield return new GeneratedFile(Path.Combine("optionsets", $"{optionset.OptionsetName}.cs"), enumResult);
            }
        }

        // --- Extracted Helper Methods ---

        private Dictionary<string, HashSet<ColumnSignature>> BuildTableColumns(IEnumerable<TableModel> tables)
        {
            var tableColumns = new Dictionary<string, HashSet<ColumnSignature>>();
            foreach (var t in tables)
            {
                var set = new HashSet<ColumnSignature>();
                foreach (var c in t.Columns)
                {
                    set.Add(new ColumnSignature(c.SchemaName, c.TypeName));
                }

                tableColumns[t.LogicalName] = set;
            }
            
            return tableColumns;
        }

        private (Dictionary<string, HashSet<ColumnSignature>> interfaceColumns, Dictionary<string, List<string>> tableToInterfaces)
            BuildIntersectionData(Dictionary<string, List<string>> intersectMapping, Dictionary<string, TableModel> tableDict, Dictionary<string, HashSet<ColumnSignature>> tableColumns)
        {
            var interfaceColumns = new Dictionary<string, HashSet<ColumnSignature>>();
            var tableToInterfaces = new Dictionary<string, List<string>>();

            if (intersectMapping != null && intersectMapping.Count > 0)
            {
                foreach (var kvp in intersectMapping)
                {
                    var interfaceName = kvp.Key;
                    var tableNames = kvp.Value.Where(tableDict.ContainsKey).ToList();
                    if (tableNames.Count == 0) continue;

                    var sets = tableNames.Select(n => tableColumns[n]).ToList();
                    var intersection = new HashSet<ColumnSignature>(sets[0]);
                    foreach (var s in sets.Skip(1))
                        intersection.IntersectWith(s);

                    if (intersection.Count > 0)
                    {
                        interfaceColumns[interfaceName] = intersection;
                    }

                    foreach (var tableName in tableNames)
                    {
                        if (!tableToInterfaces.TryGetValue(tableName, out var list))
                        {
                            list = new List<string>();
                            tableToInterfaces[tableName] = list;
                        }
                        if (!list.Contains(interfaceName))
                            list.Add(interfaceName);
                    }
                }
            }

            return (interfaceColumns, tableToInterfaces);
        }

        private IEnumerable<GeneratedFile> GenerateIntersectionInterfaceFiles(
            Dictionary<string, HashSet<ColumnSignature>> interfaceColumns,
            IEnumerable<TableModel> tables,
            string @namespace,
            Template interfaceTemplate)
        {
            foreach (var kvp in interfaceColumns)
            {
                var interfaceName = kvp.Key;
                var colSigs = kvp.Value;
                var columns = new List<object>();
                foreach (var sig in colSigs)
                {
                    var col = tables.SelectMany(t => t.Columns)
                        .FirstOrDefault(c => c.SchemaName == sig.SchemaName && c.TypeName == sig.TypeName);
                    if (col != null)
                    {
                        columns.Add(new
                        {
                            col.SchemaName,
                            col.DisplayName,
                            col.Description,
                            TypeSignature = GetPropertyTypeSignature(col)
                        });
                    }
                }
                var interfaceResult = interfaceTemplate.Render(new
                {
                    interfaceName,
                    @namespace,
                    columns
                }, member => member.Name);

                yield return new GeneratedFile(Path.Combine("intersections", $"{interfaceName}.cs"), interfaceResult);
            }
        }

        private string GetPropertyTypeSignature(ColumnModel col)
        {
            switch (col.TypeName)
            {
                case "StringColumnModel":
                case "MemoColumnModel":
                    return "string";
                case "IntegerColumnModel":
                    return col.IsNullable ? "int?" : "int";
                case "BigIntColumnModel":
                    return col.IsNullable ? "long?" : "long";
                case "BooleanColumnModel":
                    return col.IsNullable ? "bool?" : "bool";
                case "DateTimeColumnModel":
                    return col.IsNullable ? "DateTime?" : "DateTime";
                case "DecimalColumnModel":
                    return col.IsNullable ? "decimal?" : "decimal";
                case "DoubleColumnModel":
                    return col.IsNullable ? "double?" : "double";
                case "MoneyColumnModel":
                    return col.IsNullable ? "decimal?" : "decimal";
                case "EnumColumnModel":
                    var enumName = (col as EnumColumnModel)?.OptionsetName ?? "int";
                    return col.IsNullable ? $"{enumName}?" : enumName;
                case "LookupColumnModel":
                    return "EntityReference";
                case "FileColumnModel":
                case "ImageColumnModel":
                    return "byte[]";
                case "UniqueIdentifierColumnModel":
                    return col.IsNullable ? "System.Guid?" : "System.Guid";
                default:
                    return "object";
            }
        }
    }
}
