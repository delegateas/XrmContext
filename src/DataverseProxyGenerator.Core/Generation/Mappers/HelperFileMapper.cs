namespace DataverseProxyGenerator.Core.Generation.Mappers;

public static class HelperFileMapper
{
    public static object MapToTemplateModel(string templateName, GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new
        {
            @namespace = context.Namespace,
            version = context.Version,
        };
    }
}
