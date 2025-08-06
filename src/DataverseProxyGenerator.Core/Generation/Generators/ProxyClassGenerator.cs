using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class ProxyClassGenerator : BaseFileGenerator, IFileGenerator<(TableModel Table, IReadOnlyList<string> Interfaces)>
{
    public IEnumerable<GeneratedFile> Generate((TableModel Table, IReadOnlyList<string> Interfaces) input, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input.Table);
        return GenerateInternal(input, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal((TableModel Table, IReadOnlyList<string> Interfaces) input, GenerationContext context)
    {
        ValidateContext(context);
        var (table, interfaces) = input;
        var template = context.Templates.GetTemplate("ProxyClass.scriban-cs");
        var sanitizedSchemaName = SanitizeName(table.SchemaName);

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
            @namespace = context.Namespace,
            version = context.Version,
        };

        var templateContext = CreateTemplateContext(model);
        var result = template.Render(templateContext);
        yield return new GeneratedFile(FilePathHelper.GetTableFilePath(sanitizedSchemaName), result);
    }
}