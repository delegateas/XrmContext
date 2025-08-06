using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Generators;
using DataverseProxyGenerator.Core.Templates;

namespace DataverseProxyGenerator.Core.Generation;

public class CSharpProxyGenerator : ICodeGenerator
{
    private readonly EmbeddedTemplateProvider templateProvider;
    private readonly ProxyClassGenerator proxyClassGenerator;
    private readonly EnumGenerator enumGenerator;
    private readonly IntersectionInterfaceGenerator intersectionInterfaceGenerator;
    private readonly XrmContextGenerator xrmContextGenerator;
    private readonly HelperFileGenerator helperFileGenerator;

    // Helper struct for fast column comparison
    private readonly record struct ColumnSignature(string SchemaName, string TypeName);

    public CSharpProxyGenerator()
    {
        templateProvider = new EmbeddedTemplateProvider();
        proxyClassGenerator = new ProxyClassGenerator();
        enumGenerator = new EnumGenerator();
        intersectionInterfaceGenerator = new IntersectionInterfaceGenerator();
        xrmContextGenerator = new XrmContextGenerator();
        helperFileGenerator = new HelperFileGenerator();
    }

    private static string GetAssemblyVersion()
    {
        var assembly = typeof(CSharpProxyGenerator).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0.0";
    }

    public IEnumerable<GeneratedFile> GenerateCode(IEnumerable<TableModel> tables, XrmGenerationConfig config)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(config);

        var files = new List<GeneratedFile>();
        var version = GetAssemblyVersion();

        var context = new GenerationContext
        {
            Namespace = config.NamespaceSetting ?? "DataverseContext",
            Version = version,
            Templates = templateProvider,
            ServiceContextName = config.ServiceContextName,
            IntersectMapping = config.IntersectMapping,
        };

        var tablesList = tables.ToList();
        var tableDict = tablesList.ToDictionary(t => t.LogicalName, t => t, StringComparer.InvariantCulture);
        var tableColumns = BuildTableColumns(tablesList);
        var (interfaceColumns, tableToInterfaces) = BuildIntersectionData(config.IntersectMapping, tableDict, tableColumns);

        // Generate intersection interfaces
        foreach (var kvp in interfaceColumns)
        {
            var interfaceName = kvp.Key;
            var colSigs = kvp.Value;
            var columns = colSigs.Select(sig =>
                tablesList.SelectMany(t => t.Columns)
                    .FirstOrDefault(c => c.SchemaName == sig.SchemaName && c.TypeName == sig.TypeName))
                .Where(c => c != null)
                .Cast<ColumnModel>();

            files.AddRange(intersectionInterfaceGenerator.Generate((interfaceName, columns), context));
        }

        // Generate proxy classes
        foreach (var table in tablesList)
        {
            var interfaces = tableToInterfaces.TryGetValue(table.LogicalName, out var ifaces) ? ifaces : new List<string>();
            files.AddRange(proxyClassGenerator.Generate((table, interfaces), context));
        }

        // Generate enums
        var globalOptionsets = GetGlobalOptionsets(tablesList);
        foreach (var optionset in globalOptionsets)
        {
            files.AddRange(enumGenerator.Generate(optionset, context));
        }

        // Generate Xrm context class
        files.AddRange(xrmContextGenerator.Generate(tablesList, context));

        // Generate helper files
        files.AddRange(helperFileGenerator.Generate("OptionSetMetadataAttribute", context));
        files.AddRange(helperFileGenerator.Generate("RelationshipMetadataAttribute", context));
        files.AddRange(helperFileGenerator.Generate("TableAttributeHelpers", context));
        files.AddRange(helperFileGenerator.Generate("ExtendedEntity", context));

        return files;
    }

    private static IEnumerable<EnumColumnModel> GetGlobalOptionsets(IEnumerable<TableModel> tables)
    {
        return tables
            .SelectMany(t => t.Columns)
            .OfType<EnumColumnModel>()
            .Where(c => !string.IsNullOrEmpty(c.OptionsetName) && c.OptionsetValues != null)
            .GroupBy(c => c.OptionsetName, StringComparer.InvariantCulture)
            .Select(g => g.First());
    }

    // --- Extracted Helper Methods ---
    private static Dictionary<string, HashSet<ColumnSignature>> BuildTableColumns(IEnumerable<TableModel> tables)
    {
        var tableColumns = new Dictionary<string, HashSet<ColumnSignature>>(StringComparer.InvariantCulture);
        foreach (var t in tables)
        {
            var set = new HashSet<ColumnSignature>();
            foreach (var c in t.Columns)
            {
                // For EnumColumnModel, include OptionsetName in the signature to distinguish enums with same logical name but different optionsets
                if (c is EnumColumnModel enumCol)
                    set.Add(new ColumnSignature(c.SchemaName, $"EnumColumnModel:{enumCol.OptionsetName}"));
                else
                    set.Add(new ColumnSignature(c.SchemaName, c.TypeName));
            }

            tableColumns[t.LogicalName] = set;
        }

        return tableColumns;
    }

    private static (Dictionary<string, HashSet<ColumnSignature>> InterfaceColumns, Dictionary<string, List<string>> TableToInterfaces)
        BuildIntersectionData(IReadOnlyDictionary<string, IReadOnlyList<string>> intersectMapping, Dictionary<string, TableModel> tableDict, Dictionary<string, HashSet<ColumnSignature>> tableColumns)
    {
        var interfaceColumns = new Dictionary<string, HashSet<ColumnSignature>>(StringComparer.InvariantCulture);
        var tableToInterfaces = new Dictionary<string, List<string>>(StringComparer.InvariantCulture);

        if (intersectMapping != null && intersectMapping.Count > 0)
        {
            foreach (var kvp in intersectMapping)
            {
                var interfaceName = kvp.Key;
                var tableNames = kvp.Value.Where(tableDict.ContainsKey).ToList();
                if (tableNames.Count == 0)
                    continue;

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

                    if (!list.Contains(interfaceName, StringComparer.InvariantCulture))
                        list.Add(interfaceName);
                }
            }
        }

        return (interfaceColumns, tableToInterfaces);
    }
}