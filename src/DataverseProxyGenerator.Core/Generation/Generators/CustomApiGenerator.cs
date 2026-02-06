using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class CustomApiGenerator : BaseFileGenerator, IFileGenerator<CustomApiModel>
{
    public IEnumerable<GeneratedFile> Generate(CustomApiModel customApi, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(customApi);
        ArgumentNullException.ThrowIfNull(context);
        return GenerateInternal(customApi, context);
    }

    private static IEnumerable<GeneratedFile> GenerateInternal(CustomApiModel customApi, GenerationContext context)
    {
        ValidateContext(context);

        var files = new List<GeneratedFile>();
        var apiModel = CustomApiMapper.MapToTemplateModel(customApi, context);
        var templateModel = new
        {
            api = apiModel,
            @namespace = context.Namespace,
            version = context.Version,
        };

        var sanitizedUniqueName = GenerationUtilities.SanitizeName(customApi.UniqueName);

        /* Generate request class */
        var requestTemplate = context.Templates.GetTemplate("CustomApiRequest.scriban-cs");

        // Use TemplateContext with loader to support includes
        var requestTemplateContext = CreateTemplateContext(templateModel, context.Templates);
        var requestResult = requestTemplate.Render(requestTemplateContext);
        var requestFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{sanitizedUniqueName}Request.cs");

        files.Add(new GeneratedFile(requestFilename, requestResult));

        /* Generate response class */
        var responseTemplate = context.Templates.GetTemplate("CustomApiResponse.scriban-cs");

        // Use TemplateContext with loader to support includes
        var responseTemplateContext = CreateTemplateContext(templateModel, context.Templates);
        var responseResult = responseTemplate.Render(responseTemplateContext);
        var responseFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{sanitizedUniqueName}Response.cs");

        files.Add(new GeneratedFile(responseFilename, responseResult));

        return files;
    }
}