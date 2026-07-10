using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Tests;

public sealed class RetrieveMethodTests
{
    [Fact]
    public async Task EntityClass_ShouldGenerateStaticRetrieveMethod()
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
        var files = generator.GenerateCodeAsync(
            [table],
            [],
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = await files.FirstOrDefaultAsync(f => f.Filename.EndsWith("Account.cs", StringComparison.InvariantCulture));

        // Assert
        file.Should().NotBeNull();
        file!.Content.Should().Contain("using System.Linq.Expressions;");
        file.Content.Should().Contain("public static Account Retrieve(IOrganizationService service, Guid id, params Expression<Func<Account, object?>>[] columns)");
        file.Content.Should().Contain("return service.Retrieve(id, columns);");
    }

    [Fact]
    public async Task EntityClass_ShouldGenerateStaticGetColumnNameMethod()
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
        var files = generator.GenerateCodeAsync(
            new[] { table },
            Enumerable.Empty<CustomApiModel>(),
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = await files.FirstOrDefaultAsync(f => f.Filename.EndsWith("Account.cs", StringComparison.InvariantCulture));

        // Assert
        file.Should().NotBeNull();
        file!.Content.Should().Contain("using System.Linq.Expressions;");
        file.Content.Should().Contain("/// <summary>");
        file.Content.Should().Contain("/// Gets the logical column name for a property on the Account entity, using the AttributeLogicalNameAttribute if present.");
        file.Content.Should().Contain("/// </summary>");
        file.Content.Should().Contain("/// <param name=\"columns\">Expressions that specify columns to retrieve</param>");
        file.Content.Should().Contain("/// <returns>Name of column</returns>");
        file.Content.Should().Contain("/// <exception cref=\"ArgumentNullException\">If no expression is provided</exception>");
        file.Content.Should().Contain("/// <exception cref=\"ArgumentException\">If the expression is not x => x.column</exception>");
        file.Content.Should().Contain("public static string GetColumnName(Expression<Func<Account, object?>> column)");
        file.Content.Should().Contain("return TableAttributeHelpers.GetColumnName(column);");
    }

    [Fact]
    public async Task TableAttributeHelpers_ShouldGenerateRetrieveExtensionMethod()
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
        var files = generator.GenerateCodeAsync(
            new[] { table },
            Enumerable.Empty<CustomApiModel>(),
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = await files.FirstOrDefaultAsync(f => f.Filename.EndsWith("TableAttributeHelpers.cs", StringComparison.InvariantCulture));

        // Assert
        file.Should().NotBeNull();
        file!.Content.Should().Contain("using Microsoft.Xrm.Sdk.Query;");
        file.Content.Should().Contain("public static T Retrieve<T>(this IOrganizationService service, Guid id, params Expression<Func<T, object?>>[] attrs)");
        file.Content.Should().Contain("where T : Entity, new()");
        file.Content.Should().Contain("var columnNames = attrs.Select(attr => GetColumnName(attr)).ToArray();");
        file.Content.Should().Contain("return service.Retrieve(entityLogicalName, id, columnSet).ToEntity<T>();");
    }

    [Fact]
    public async Task EntityClass_ShouldGenerateAlternateKeyRetrieveMethods()
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
                new IntegerColumnModel
                {
                    SchemaName = "AccountNumber",
                    LogicalName = "new_accountnumber",
                    DisplayName = "Account Number",
                    Min = 0,
                    Max = 999999,
                },
            },
            Relationships = new List<RelationshipModel>(),
            Keys = new List<AlternateKeyModel>
            {
                new AlternateKeyModel
                {
                    SchemaName = "ThisKey",
                    DisplayName = "This Key",
                    KeyAttributes = new List<ColumnModel>
                    {
                        new StringColumnModel
                        {
                            SchemaName = "Name",
                            LogicalName = "name",
                            DisplayName = "Name",
                            MaxLength = 100,
                        },
                        new IntegerColumnModel
                        {
                            SchemaName = "AccountNumber",
                            LogicalName = "new_accountnumber",
                            DisplayName = "Account Number",
                            Min = 0,
                            Max = 999999,
                        },
                    },
                },
            },
        };

        var generator = new CSharpProxyGenerator();

        // Act
        var files = generator.GenerateCodeAsync(
            new[] { table },
            Enumerable.Empty<CustomApiModel>(),
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = await files.FirstOrDefaultAsync(f => f.Filename.EndsWith("Account.cs", StringComparison.InvariantCulture));

        // Assert
        file.Should().NotBeNull();
        file!.Content.Should().Contain("public static Account Retrieve_ThisKey(IOrganizationService service, string Name, int AccountNumber, params Expression<Func<Account, object?>>[] columns)");
        file.Content.Should().Contain("var keyedEntityReference = new EntityReference(EntityLogicalName, new KeyAttributeCollection");
        file.Content.Should().Contain("[\"name\"] = Name,");
        file.Content.Should().Contain("[\"new_accountnumber\"] = AccountNumber");
        file.Content.Should().Contain("return service.Retrieve(keyedEntityReference, columns);");
    }
}