using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Tests;

public sealed class SingleFileTemplateTests
{
    [Fact]
    public async Task SingleFile_Template_Generates_XRM_Service_Context_Correctly()
    {
        // Arrange
        var tables = new List<TableModel>
        {
            new TableModel
            {
                SchemaName = "Account",
                LogicalName = "account",
                DisplayName = "Account",
                Columns = new List<ColumnModel>
                {
                    new StringColumnModel { LogicalName = "name", SchemaName = "Name", DisplayName = "Name" },
                },
            },
            new TableModel
            {
                SchemaName = "Contact",
                LogicalName = "contact",
                DisplayName = "Contact",
                Columns = new List<ColumnModel>
                {
                    new StringColumnModel { LogicalName = "fullname", SchemaName = "FullName", DisplayName = "Full Name" },
                },
            },
        };

        var config = new XrmGenerationConfig(
            OutputDirectory: "test",
            NamespaceSetting: "TestNamespace",
            ServiceContextName: "TestXrm",
            IntersectMapping: new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
            SingleFile: true,
            GenerateCustomApis: false);

        var generator = new CSharpProxyGenerator();

        // Act
        var singleFileResults = await generator.GenerateCodeAsync(tables, Enumerable.Empty<CustomApiModel>(), config).ToListAsync();

        // Test with multiple file generation for comparison
        var multiFileConfig = new XrmGenerationConfig(
            OutputDirectory: "test",
            NamespaceSetting: "TestNamespace",
            ServiceContextName: "TestXrm",
            IntersectMapping: new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
            SingleFile: false,
            GenerateCustomApis: false);

        var multiFileResults = await generator.GenerateCodeAsync(tables, Enumerable.Empty<CustomApiModel>(), multiFileConfig).ToListAsync();

        // Assert
        Assert.Single(singleFileResults);
        var singleFileContent = singleFileResults[0].Content;

        // Verify XRM Service Context is generated
        Assert.Contains("public class TestXrm : OrganizationServiceContext", singleFileContent, StringComparison.Ordinal);
        Assert.Contains("public IQueryable<Account> AccountSet", singleFileContent, StringComparison.Ordinal);
        Assert.Contains("public IQueryable<Contact> ContactSet", singleFileContent, StringComparison.Ordinal);
        Assert.Contains("return CreateQuery<Account>()", singleFileContent, StringComparison.Ordinal);
        Assert.Contains("return CreateQuery<Contact>()", singleFileContent, StringComparison.Ordinal);

        // Verify the service context name is not null/empty
        Assert.DoesNotContain("public class  : OrganizationServiceContext", singleFileContent, StringComparison.Ordinal);

        // Find the XRM context file in multi-file results
        var xrmContextFile = multiFileResults.Find(f => f.Filename.Contains("TestXrm", StringComparison.Ordinal));
        Assert.NotNull(xrmContextFile);

        // Extract just the service context class from both
        var singleFileServiceContext = ExtractServiceContextClass(singleFileContent);
        var multiFileServiceContext = ExtractServiceContextClass(xrmContextFile.Content);

        // The service context parts should be functionally equivalent
        Assert.Contains("public class TestXrm : OrganizationServiceContext", singleFileServiceContext, StringComparison.Ordinal);
        Assert.Contains("public class TestXrm : OrganizationServiceContext", multiFileServiceContext, StringComparison.Ordinal);
        Assert.Contains("public IQueryable<Account> AccountSet", singleFileServiceContext, StringComparison.Ordinal);
        Assert.Contains("public IQueryable<Contact> ContactSet", singleFileServiceContext, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleFile_Template_Uses_Default_ServiceContextName_When_Null()
    {
        // Arrange
        var tables = new List<TableModel>
        {
            new TableModel
            {
                SchemaName = "Account",
                LogicalName = "account",
                DisplayName = "Account",
                Columns = new List<ColumnModel>
                {
                    new StringColumnModel { LogicalName = "name", SchemaName = "Name", DisplayName = "Name" },
                },
            },
        };

        var config = new XrmGenerationConfig(
            OutputDirectory: "test",
            NamespaceSetting: "TestNamespace",
            ServiceContextName: null!, // This should default to "Xrm"
            IntersectMapping: new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture),
            SingleFile: true,
            GenerateCustomApis: false);

        var generator = new CSharpProxyGenerator();

        // Act
        var results = await generator.GenerateCodeAsync(tables, Enumerable.Empty<CustomApiModel>(), config).ToListAsync();

        // Assert
        Assert.Single(results);
        var content = results[0].Content;

        // Verify default service context name is used
        Assert.Contains("public class Xrm : OrganizationServiceContext", content, StringComparison.Ordinal);
        Assert.DoesNotContain("public class  : OrganizationServiceContext", content, StringComparison.Ordinal);
    }

    private static string ExtractServiceContextClass(string content)
    {
        var startMarker = "// XRM SERVICE CONTEXT";

        var startIndex = content.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIndex == -1)
        {
            // Try alternative marker
            startMarker = "public class";
            startIndex = content.LastIndexOf(startMarker, StringComparison.Ordinal);
        }

        if (startIndex == -1)
        {
            return string.Empty;
        }

        var substring = content.Substring(startIndex);
        var lines = substring.Split('\n');
        var classLines = new List<string>();
        var braceCount = 0;
        var foundClass = false;

        foreach (var line in lines)
        {
            if (line.Contains("public class", StringComparison.Ordinal) && line.Contains("OrganizationServiceContext", StringComparison.Ordinal))
            {
                foundClass = true;
            }

            if (foundClass)
            {
                classLines.Add(line);
                braceCount += line.Count(c => c == '{');
                braceCount -= line.Count(c => c == '}');

                if (braceCount == 0 && line.Contains('}', StringComparison.Ordinal))
                {
                    break;
                }
            }
        }

        return string.Join('\n', classLines);
    }
}