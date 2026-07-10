using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation.Common;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Core.Generation.Generators;

public class CustomApiGenerator : BaseFileGenerator, IFileGenerator<CustomApiModel>
{
    public IAsyncEnumerable<GeneratedFile> GenerateAsync(CustomApiModel customApi, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(customApi);
        ArgumentNullException.ThrowIfNull(context);
        return GenerateInternalAsync(customApi, context);
    }

    private static async IAsyncEnumerable<GeneratedFile> GenerateInternalAsync(CustomApiModel customApi, GenerationContext context)
    {
        ValidateContext(context);

        var apiModel = CustomApiMapper.MapToTemplateModel(customApi, context);
        var templateModel = new
        {
            api = apiModel,
            @namespace = context.Namespace,
            version = context.Version,
        };

        var sanitizedUniqueName = GenerationUtilities.SanitizeName(customApi.UniqueName);

        /* Generate request class */
        var requestTemplate = await context.Templates.GetTemplateAsync("CustomApiRequest.scriban-cs");

        // Use TemplateContext with loader to support includes
        var requestTemplateContext = CreateTemplateContext(templateModel, context.Templates);
        var requestResult = await requestTemplate.RenderAsync(requestTemplateContext);
        var requestFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{sanitizedUniqueName}Request.cs");

        yield return new GeneratedFile(requestFilename, requestResult);

        /* Generate response class */
        var responseTemplate = await context.Templates.GetTemplateAsync("CustomApiResponse.scriban-cs");

        // Use TemplateContext with loader to support includes
        var responseTemplateContext = CreateTemplateContext(templateModel, context.Templates);
        var responseResult = await responseTemplate.RenderAsync(responseTemplateContext);
        var responseFilename = Path.Combine(FilePathHelper.CustomApiPath, $"{sanitizedUniqueName}Response.cs");

        yield return new GeneratedFile(responseFilename, responseResult);
    }
}