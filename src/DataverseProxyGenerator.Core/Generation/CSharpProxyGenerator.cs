using DataverseProxyGenerator.Core.Domain;
using Scriban;

namespace DataverseProxyGenerator.Core.Generation;

public class CSharpProxyGenerator : ICodeGenerator
{
    // Helper struct for fast column comparison
    private readonly record struct ColumnSignature(string SchemaName, string TypeName);

    private static string GetEmbeddedResourceText(string resourceName)
    {
        var assembly = typeof(CSharpProxyGenerator).Assembly;
        var fullResourceName = $"DataverseProxyGenerator.Core.Templates.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
            throw new FileNotFoundException($"Could not find embedded resource: {fullResourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ProxyClassTemplate => GetEmbeddedResourceText("ProxyClass.scriban-cs");

    private static string EnumTemplate => GetEmbeddedResourceText("EnumOptionset.scriban-cs");

    private static string IntersectionInterfaceTemplate => GetEmbeddedResourceText("IntersectionInterface.scriban-cs");

    private static string OptionSetMetadataAttributeTemplate => GetEmbeddedResourceText("OptionSetMetadataAttribute.scriban-cs");

    private static string RelationshipMetadataAttributeTemplate => GetEmbeddedResourceText("RelationshipMetadataAttribute.scriban-cs");

    private static string XrmClassTemplate => GetEmbeddedResourceText("XrmClass.scriban-cs");

    private static string TableHelperTemplate => GetEmbeddedResourceText("TableAttributeHelpers.scriban-cs");

    private static string ExtendedEntityTemplate => GetEmbeddedResourceText("ExtendedEntity.scriban-cs");

    public IEnumerable<GeneratedFile> GenerateCode(IEnumerable<TableModel> tables, XrmGenerationConfig config)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(config);

        var templates = LoadAllTemplates();

        var files = new List<GeneratedFile>();

        var tableDict = tables.ToDictionary(t => t.LogicalName, t => t, StringComparer.InvariantCulture);
        var tableColumns = BuildTableColumns(tables);

        var (interfaceColumns, tableToInterfaces) = BuildIntersectionData(config.IntersectMapping, tableDict, tableColumns);

        files.AddRange(GenerateIntersectionInterfaceFiles(interfaceColumns, tables, config.NamespaceSetting, templates.InterfaceTemplate));

        // Generate proxy classes (with interfaces if needed)
        files.AddRange(GenerateProxyClassFiles(tables, config.NamespaceSetting, tableToInterfaces, templates.ProxyTemplate));

        // Generate enums as before
        files.AddRange(GenerateEnumFiles(GetGlobalOptionsets(tables), config.NamespaceSetting, templates.EnumTemplate));

        // Generate Xrm context class
        var xrmClassResult = templates.XrmTemplate.Render(new { tables, @namespace = config.NamespaceSetting, config.ServiceContextName }, member => member.Name);
        files.Add(new GeneratedFile(Path.Combine("queries", "Xrm.cs"), xrmClassResult));

        // Generate OptionSetMetadataAttribute
        var attributeResult = templates.OptionSetMetadataAttributeTemplate.Render(new { @namespace = config.NamespaceSetting }, member => member.Name);
        files.Add(new GeneratedFile(Path.Combine("attributes", "OptionSetMetadataAttribute.cs"), attributeResult));

        // Generate RelationshipMetadataAttribute
        var relationshipAttributeResult = templates.RelationshipMetadataAttributeTemplate.Render(new { @namespace = config.NamespaceSetting }, member => member.Name);
        files.Add(new GeneratedFile(Path.Combine("attributes", "RelationshipMetadataAttribute.cs"), relationshipAttributeResult));

        // Generate TableAttributeHelpers
        var tableHelperResult = templates.TableHelperTemplate.Render(new { @namespace = config.NamespaceSetting }, member => member.Name);
        files.Add(new GeneratedFile(Path.Combine("tables", "TableAttributeHelpers.cs"), tableHelperResult));

        // Generate ExtendedEntity
        var extendedEntityResult = templates.ExtendedEntityTemplate.Render(new { @namespace = config.NamespaceSetting }, member => member.Name);
        files.Add(new GeneratedFile(Path.Combine("tables", "ExtendedEntity.cs"), extendedEntityResult));

        return files;
    }

    private static IEnumerable<GeneratedFile> GenerateProxyClassFiles(
        IEnumerable<TableModel> tables,
        string @namespace,
        Dictionary<string, List<string>> tableToInterfaces,
        Template proxyTemplate)
    {
        foreach (var table in tables)
        {
            var interfaces = tableToInterfaces.TryGetValue(table.LogicalName, out var ifaces) ? ifaces : new List<string>();
            var model = new
            {
                table = new
                {
                    SchemaName = table.SchemaName,
                    Columns = table.Columns.Select(c =>
                    c switch
                    {
                        EnumColumnModel enumCol => enumCol with
                        {
                            SchemaName = SanitizeName(enumCol.SchemaName),
                            OptionsetName = SanitizeName(enumCol.OptionsetName),
                        },
                        _ => c with
                        {
                            SchemaName = SanitizeName(c.SchemaName),
                        },
                    }),
                    Relationships = table.Relationships.Select(r => r with
                    {
                        SchemaName = SanitizeName(r.SchemaName),
                    }),
                    LogicalName = table.LogicalName,
                    DisplayName = table.DisplayName,
                    EntityTypeCode = table.EntityTypeCode,
                    PrimaryNameAttribute = table.PrimaryNameAttribute,
                    PrimaryIdAttribute = table.PrimaryIdAttribute,
                    IsIntersect = table.IsIntersect,
                    InterfacesList = interfaces ?? new List<string>(),
                },
                @namespace,
            };
            var context = new Scriban.TemplateContext(StringComparer.InvariantCulture);
            context.LoopLimit = 0; // 0 means no limit
            context.MemberRenamer = member => member.Name;
            context.PushGlobal(Scriban.Runtime.ScriptObject.From(model));
            var result = proxyTemplate.Render(context);
            yield return new GeneratedFile(Path.Combine("tables", $"{table.SchemaName}.cs"), result);
        }
    }

    private static (Template ProxyTemplate,
        Template EnumTemplate,
        Template InterfaceTemplate,
        Template OptionSetMetadataAttributeTemplate,
        Template RelationshipMetadataAttributeTemplate,
        Template XrmTemplate,
        Template TableHelperTemplate,
        Template ExtendedEntityTemplate)
        LoadAllTemplates()
    {
        var proxyTemplate = Template.Parse(ProxyClassTemplate);
        var enumTemplate = Template.Parse(EnumTemplate);
        var interfaceTemplate = Template.Parse(IntersectionInterfaceTemplate);
        var optionSetMetadataAttributeTemplate = Template.Parse(OptionSetMetadataAttributeTemplate);
        var relationshipMetadataAttributeTemplate = Template.Parse(RelationshipMetadataAttributeTemplate);
        var xrmClassTemplate = Template.Parse(XrmClassTemplate);
        var tableHelperTemplate = Template.Parse(TableHelperTemplate);
        var extendedEntityTemplate = Template.Parse(ExtendedEntityTemplate);

        return (proxyTemplate,
            enumTemplate,
            interfaceTemplate,
            optionSetMetadataAttributeTemplate,
            relationshipMetadataAttributeTemplate,
            xrmClassTemplate,
            tableHelperTemplate,
            extendedEntityTemplate);
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

    private static IEnumerable<GeneratedFile> GenerateEnumFiles(IEnumerable<EnumColumnModel> globalOptionsets, string @namespace, Template enumTemplate)
    {
        foreach (var optionset in globalOptionsets)
        {
            var enumResult = enumTemplate.Render(
                new
                {
                    optionsetName = SanitizeName(optionset.OptionsetName),
                    optionsetValues = optionset.OptionsetValues.Select(kvp => new
                    {
                        Value = kvp.Key,
                        Name = SanitizeName(kvp.Value),
                        Localizations =
                            optionset.OptionLocalizations != null &&
                            optionset.OptionLocalizations.TryGetValue(kvp.Key, out var value)
                            ? value : [],
                    }),
                    @namespace,
                },
                member => member.Name);

            yield return new GeneratedFile(Path.Combine("optionsets", $"{SanitizeName(optionset.OptionsetName)}.cs"), enumResult);
        }
    }

    // --- Enum Name Sanitization Helper ---
    private static readonly char[] CharsToRemove = { '(', ')', '\'', '-', '–', '%' };

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            var rand = new System.Random();
#pragma warning disable SCS0005 // Weak random number generator.
#pragma warning disable CA5394 // Do not use insecure randomness
            return "EmptyName" + rand.Next(1000, 10000);
#pragma warning restore CA5394 // Do not use insecure randomness
#pragma warning restore SCS0005 // Weak random number generator.
        }

        // Prepend with special character if name starts with digit
        var cleaned = string.Concat(name.Where(c => !CharsToRemove.Contains(c)));
        if (cleaned.Length > 0 && char.IsDigit(cleaned[0]))
        {
            cleaned = "X" + cleaned;
        }

        return cleaned;
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

    private static IEnumerable<GeneratedFile> GenerateIntersectionInterfaceFiles(
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
                        SchemaName = SanitizeName(col.SchemaName),
                        col.DisplayName,
                        col.Description,
                        TypeSignature = GetPropertyTypeSignature(col),
                    });
                }
            }

            var interfaceResult = interfaceTemplate.Render(
                new
                {
                    interfaceName,
                    @namespace,
                    columns,
                },
                member => member.Name);

            yield return new GeneratedFile(Path.Combine("intersections", $"{interfaceName}.cs"), interfaceResult);
        }
    }

    private static string GetPropertyTypeSignature(ColumnModel col)
    {
        switch (col.TypeName)
        {
            case "StringColumnModel":
            case "MemoColumnModel":
                return "string?";
            case "IntegerColumnModel":
                return "int?";
            case "BigIntColumnModel":
                return "long?";
            case "BooleanColumnModel":
                return "bool?";
            case "DateTimeColumnModel":
                return "DateTime?";
            case "DecimalColumnModel":
                return "decimal?";
            case "DoubleColumnModel":
                return "double?";
            case "MoneyColumnModel":
                return "decimal?";
            case "EnumColumnModel":
                var enumName = SanitizeName(((EnumColumnModel)col).OptionsetName);
                return $"{enumName}?";
            case "LookupColumnModel":
                return "EntityReference?";
            case "PartyListColumnModel":
                return "IEnumerable<ActivityParty>";
            case "FileColumnModel":
            case "ImageColumnModel":
                return "byte[]";
            case "PrimaryIdColumnModel":
                return "Guid";
            default:
                return "object";
        }
    }
}
