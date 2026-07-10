using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class SingleFileGenerator : BaseFileGenerator, IFileGenerator<(IReadOnlyList<TableModel> Tables, IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> InterfaceColumns, IReadOnlyDictionary<string, IReadOnlyList<string>> TableToInterfaces, IReadOnlyList<CustomApiModel> CustomApis)>
{
    public IAsyncEnumerable<GeneratedFile> GenerateAsync(
        (IReadOnlyList<TableModel> Tables, IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> InterfaceColumns, IReadOnlyDictionary<string, IReadOnlyList<string>> TableToInterfaces, IReadOnlyList<CustomApiModel> CustomApis) input,
        GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return GenerateInternalAsync(input.Tables, input.InterfaceColumns, input.TableToInterfaces, input.CustomApis, context);
    }

    private static async IAsyncEnumerable<GeneratedFile> GenerateInternalAsync(
        IReadOnlyList<TableModel> tablesList,
        IReadOnlyDictionary<string, IReadOnlySet<ColumnSignature>> interfaceColumns,
        IReadOnlyDictionary<string, IReadOnlyList<string>> tableToInterfaces,
        IReadOnlyList<CustomApiModel> customApis,
        GenerationContext context)
    {
        ValidateContext(context);

        var templateModel = SingleFileMapper.MapToTemplateModel(tablesList, interfaceColumns, tableToInterfaces, customApis, context);
        var templateName = "SingleFile.scriban-cs";
        var template = await context.Templates.GetTemplateAsync(templateName);

        // Use the same template context creation as other generators
        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var content = await template.RenderAsync(templateContext);

        yield return new GeneratedFile($"{context.ServiceContextName}.cs", content);
    }
}