using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
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

        var proxyClassModel = ProxyClassMapper.MapToTemplateModel(input, context);
        var templateModel = new
        {
            table = proxyClassModel,
            @namespace = context.Namespace,
            version = context.Version,
        };

        var template = context.Templates.GetTemplate("ProxyClass.scriban-cs");

        var templateContext = CreateTemplateContext(templateModel, context.Templates);
        var result = template.Render(templateContext);

        var sanitizedSchemaName = GenerationUtilities.SanitizeName(input.Table.SchemaName);
        yield return new GeneratedFile(FilePathHelper.GetTableFilePath(sanitizedSchemaName), result);
    }
}