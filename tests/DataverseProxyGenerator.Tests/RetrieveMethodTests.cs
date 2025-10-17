using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Tests;

public sealed class RetrieveMethodTests
{
    [Fact]
    public void EntityClass_ShouldGenerateStaticRetrieveMethod()
    {
        // Arrange
        var table = new TableModel
        {
            SchemaName = "Account",
            LogicalName = "account",
            DisplayName = "Account",
            Description = "Business account",
            EntityTypeCode = 1,
            PrimaryNameAttribute = "name",
            PrimaryIdAttribute = "accountid",
            IsIntersect = false,
            Columns = new ColumnModel[]
            {
                new StringColumnModel
                {
                    SchemaName = "Name",
                    LogicalName = "name",
                    DisplayName = "Name",
                    MaxLength = 100,
                },
            },
            Relationships = new List<RelationshipModel>(),
        };

        var generator = new CSharpProxyGenerator();

        // Act
        var files = generator.GenerateCode(
            new[] { table },
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = files.FirstOrDefault(f => f.Filename.EndsWith("Account.cs", StringComparison.InvariantCulture));

        // Assert
        file.Should().NotBeNull();
        file!.Content.Should().Contain("using System.Linq.Expressions;");
        file.Content.Should().Contain("public static Account Retrieve(IOrganizationService service, Guid id, params Expression<Func<Account, object>>[] attrs)");
        file.Content.Should().Contain("return service.Retrieve(id, attrs);");
    }

    [Fact]
    public void EntityClass_ShouldGenerateStaticGetColumnNameMethod()
    {
        // Arrange
        var table = new TableModel
        {
            SchemaName = "Account",
            LogicalName = "account",
            DisplayName = "Account",
            Description = "This is an Account",
            EntityTypeCode = 1,
            PrimaryNameAttribute = "name",
            PrimaryIdAttribute = "accountid",
            IsIntersect = false,
            Columns = new ColumnModel[]
            {
                new StringColumnModel
                {
                    SchemaName = "Name",
                    LogicalName = "name",
                    DisplayName = "Name",
                    MaxLength = 100,
                },
            },
            Relationships = new List<RelationshipModel>(),
        };

        var generator = new CSharpProxyGenerator();

        // Act
        var files = generator.GenerateCode(
            new[] { table },
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = files.FirstOrDefault(f => f.Filename.EndsWith("Account.cs", StringComparison.InvariantCulture));

        // Assert
        file.Should().NotBeNull();
        file!.Content.Should().Contain("using System.Linq.Expressions;");
        file.Content.Should().Contain("/// <summary>");
        file.Content.Should().Contain("/// Gets the logical column name for a property on the Account entity, using the AttributeLogicalNameAttribute if present.");
        file.Content.Should().Contain("/// </summary>");
        file.Content.Should().Contain("/// <param name=\"lambda\">Expression to pick the column</param>");
        file.Content.Should().Contain("/// <returns>Name of column</returns>");
        file.Content.Should().Contain("/// <exception cref=\"ArgumentNullException\">If no expression is provided</exception>");
        file.Content.Should().Contain("/// <exception cref=\"ArgumentException\">If the expression is not x => x.column</exception>");
        file.Content.Should().Contain("public static string GetColumnName(Expression<Func<Account, object>> lambda)");
        file.Content.Should().Contain("return TableAttributeHelpers.GetColumnName(lambda);");
    }

    [Fact]
    public void TableAttributeHelpers_ShouldGenerateRetrieveExtensionMethod()
    {
        // Arrange
        var table = new TableModel
        {
            SchemaName = "TestEntity",
            LogicalName = "testentity",
            DisplayName = "Test Entity",
            Description = "Test entity",
            EntityTypeCode = 1,
            PrimaryNameAttribute = "name",
            PrimaryIdAttribute = "testentityid",
            IsIntersect = false,
            Columns = new ColumnModel[]
            {
                new StringColumnModel
                {
                    SchemaName = "Name",
                    LogicalName = "name",
                    DisplayName = "Name",
                    MaxLength = 100,
                },
            },
            Relationships = new List<RelationshipModel>(),
        };

        var generator = new CSharpProxyGenerator();

        // Act
        var files = generator.GenerateCode(
            new[] { table },
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = files.FirstOrDefault(f => f.Filename.EndsWith("TableAttributeHelpers.cs", StringComparison.InvariantCulture));

        // Assert
        file.Should().NotBeNull();
        file!.Content.Should().Contain("using Microsoft.Xrm.Sdk.Query;");
        file.Content.Should().Contain("public static T Retrieve<T>(this IOrganizationService service, Guid id, params Expression<Func<T, object>>[] attrs)");
        file.Content.Should().Contain("where T : Entity, new()");
        file.Content.Should().Contain("var columnNames = attrs.Select(attr => entity.GetColumnName(attr)).ToArray();");
        file.Content.Should().Contain("return service.Retrieve(entityLogicalName, id, columnSet).ToEntity<T>();");
    }
}