using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Templates;

namespace DataverseProxyGenerator.Tests;

public class EnumGeneratorDuplicateTests
{
    [Fact]
    public void Generate_WithDuplicateLabels_ProducesUniqueEnumNames()
    {
        // Arrange
        var generator = new DataverseProxyGenerator.Core.Generation.Generators.EnumGenerator();
        var context = new GenerationContext
        {
            Templates = new EmbeddedTemplateProvider(),
            Namespace = "TestNamespace",
            Version = "1.0.0",
        };

        var enumColumn = new EnumColumnModel
        {
            LogicalName = "status",
            SchemaName = "Status",
            DisplayName = "Status",
            OptionsetName = "TestOptionSet",
            OptionsetValues = new Dictionary<int, string>
            {
                { 1, "Active" },
                { 2, "Active" }, // Duplicate label
                { 3, "Inactive" },
                { 4, "Active" }, // Another duplicate
                { 5, "Pending" },
            },
        };

        // Act
        var result = generator.Generate(enumColumn, context).ToList();

        // Assert
        Assert.Single(result);
        var generatedCode = result[0].Content;

        // Verify that all enum values are present with unique names
        Assert.Contains("Active = 1,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Active_1 = 2,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Inactive = 3,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Active_2 = 4,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Pending = 5,", generatedCode, StringComparison.Ordinal);

        // Verify no duplicate enum member names by checking each name appears only once
        var lines = generatedCode.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var enumMemberLines = lines.Where(line => line.Trim().Contains(" = ", StringComparison.Ordinal) && line.Trim().EndsWith(',')).ToList();

        var memberNames = new List<string>();
        foreach (var line in enumMemberLines)
        {
            var trimmed = line.Trim();
            var nameEnd = trimmed.IndexOf(' ', StringComparison.Ordinal);
            if (nameEnd > 0)
            {
                memberNames.Add(trimmed.Substring(0, nameEnd));
            }
        }

        // All member names should be unique
        Assert.Equal(memberNames.Count, memberNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(5, memberNames.Count); // Should have exactly 5 members
    }

    [Fact]
    public void Generate_WithAllSameLabels_ProducesIncrementallyNumberedNames()
    {
        // Arrange
        var generator = new DataverseProxyGenerator.Core.Generation.Generators.EnumGenerator();
        var context = new GenerationContext
        {
            Templates = new EmbeddedTemplateProvider(),
            Namespace = "TestNamespace",
            Version = "1.0.0",
        };

        var enumColumn = new EnumColumnModel
        {
            LogicalName = "status",
            SchemaName = "Status",
            DisplayName = "Status",
            OptionsetName = "TestOptionSet",
            OptionsetValues = new Dictionary<int, string>
            {
                { 10, "Same" },
                { 20, "Same" },
                { 30, "Same" },
            },
        };

        // Act
        var result = generator.Generate(enumColumn, context).ToList();

        // Assert
        Assert.Single(result);
        var generatedCode = result[0].Content;

        Assert.Contains("Same = 10,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Same_1 = 20,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Same_2 = 30,", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithEmptyLabels_UsesOptionValueFallbacks()
    {
        // Arrange
        var generator = new DataverseProxyGenerator.Core.Generation.Generators.EnumGenerator();
        var context = new GenerationContext
        {
            Templates = new EmbeddedTemplateProvider(),
            Namespace = "TestNamespace",
            Version = "1.0.0",
        };

        var enumColumn = new EnumColumnModel
        {
            LogicalName = "status",
            SchemaName = "Status",
            DisplayName = "Status",
            OptionsetName = "TestOptionSet",
            OptionsetValues = new Dictionary<int, string>
            {
                { 1, "" },
                { 2, string.Empty },
                { 3, "ValidLabel" },
            },
        };

        // Act
        var result = generator.Generate(enumColumn, context).ToList();

        // Assert
        Assert.Single(result);
        var generatedCode = result[0].Content;

        Assert.Contains("Option_1 = 1,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Option_2 = 2,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("ValidLabel = 3,", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithMultipleDuplicateGroups_ResetsCounterPerGroup()
    {
        // Arrange
        var generator = new DataverseProxyGenerator.Core.Generation.Generators.EnumGenerator();
        var context = new GenerationContext
        {
            Templates = new EmbeddedTemplateProvider(),
            Namespace = "TestNamespace",
            Version = "1.0.0",
        };

        var enumColumn = new EnumColumnModel
        {
            LogicalName = "status",
            SchemaName = "Status",
            DisplayName = "Status",
            OptionsetName = "TestOptionSet",
            OptionsetValues = new Dictionary<int, string>
            {
                { 1, "Active" },    // First Active
                { 2, "Pending" },   // First Pending
                { 3, "Active" },    // Second Active -> Active_1
                { 4, "Closed" },    // Only Closed
                { 5, "Pending" },   // Second Pending -> Pending_1
                { 6, "Active" },    // Third Active -> Active_2
            },
        };

        // Act
        var result = generator.Generate(enumColumn, context).ToList();

        // Assert
        Assert.Single(result);
        var generatedCode = result[0].Content;

        // Verify counter resets for each group
        Assert.Contains("Active = 1,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Pending = 2,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Active_1 = 3,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Closed = 4,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Pending_1 = 5,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Active_2 = 6,", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithSpecialCharactersInLabels_SanitizesEnumMemberNames()
    {
        // Arrange
        var generator = new DataverseProxyGenerator.Core.Generation.Generators.EnumGenerator();
        var context = new GenerationContext
        {
            Templates = new EmbeddedTemplateProvider(),
            Namespace = "TestNamespace",
            Version = "1.0.0",
        };

        var enumColumn = new EnumColumnModel
        {
            LogicalName = "paymenttermscode",
            SchemaName = "PaymentTermsCode",
            DisplayName = "Payment Terms",
            OptionsetName = "TestOptionSet",
            OptionsetValues = new Dictionary<int, string>
            {
                { 1, "Løbende måned + 14 dage" },   // Contains + character
                { 2, "Field\nWith\nNewlines" },     // Contains newline characters
                { 3, "Tab\tSeparated\tValue" },     // Contains tab characters
                { 4, "Multiple+Special\nChars." },  // Contains multiple special chars
                { 5, "Normal Value" },              // Normal case for comparison
            },
        };

        // Act
        var result = generator.Generate(enumColumn, context).ToList();

        // Assert
        Assert.Single(result);
        var generatedCode = result[0].Content;

        // Verify that special characters are removed entirely from enum member names
        Assert.Contains("Løbendemåned14dage = 1,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("FieldWithNewlines = 2,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("TabSeparatedValue = 3,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("MultipleSpecialChars = 4,", generatedCode, StringComparison.Ordinal);
        Assert.Contains("NormalValue = 5,", generatedCode, StringComparison.Ordinal);
    }
}