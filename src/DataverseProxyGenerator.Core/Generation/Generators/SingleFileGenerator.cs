using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class SingleFileGenerator : BaseFileGenerator, IFileGenerator<(IReadOnlyList<TableModel> Tables, IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> InterfaceColumns, IReadOnlyDictionary<string, IReadOnlyList<string>> TableToInterfaces)>
{
    public IEnumerable<GeneratedFile> Generate(
        (IReadOnlyList<TableModel> Tables, IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> InterfaceColumns, IReadOnlyDictionary<string, IReadOnlyList<string>> TableToInterfaces) input,
        GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return GenerateInternal(input.Tables, input.InterfaceColumns, input.TableToInterfaces, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal(
        IReadOnlyList<TableModel> tablesList,
        IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> interfaceColumns,
        IReadOnlyDictionary<string, IReadOnlyList<string>> tableToInterfaces,
        GenerationContext context)
    {
        ValidateContext(context);

        var templateModel = SingleFileMapper.MapToTemplateModel(tablesList, interfaceColumns, tableToInterfaces, context);
        var templateName = "SingleFile.scriban-cs";
        var template = context.Templates.GetTemplate(templateName);

        // Use the same template context creation as other generators
        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var content = template.Render(templateContext);

        yield return new GeneratedFile($"{context.ServiceContextName}.cs", content);
    }
}
