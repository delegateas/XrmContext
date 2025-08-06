using Scriban;

namespace DataverseProxyGenerator.Core.Templates;

public interface ITemplateProvider
{
    Template GetTemplate(string templateName);

    bool HasTemplate(string templateName);
}