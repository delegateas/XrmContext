using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Generation.Mappers;
using DataverseProxyGenerator.Core.Templates;

namespace DataverseProxyGenerator.Tests;

public sealed class ProxyClassMapperTests
{
    [Fact]
    public void MapToTemplateModel_WithPropertyNameMatchingClassName_AppendsUnderscoreOne()
    {
        // Arrange
        var table = new TableModel
        {
            SchemaName = "Account",
            LogicalName = "account",
            DisplayName = "Account",
            Description = "Test account",
            EntityTypeCode = 1,
            PrimaryNameAttribute = "name",
            PrimaryIdAttribute = "accountid",
            IsIntersect = false,
            Columns = new ColumnModel[]
            {
                new StringColumnModel
                {
                    SchemaName = "Account", // Matches class name
                    LogicalName = "account_field",
                    DisplayName = "Account Field",
                    MaxLength = 100,
                },
            },
            Relationships = new List<RelationshipModel>(),
        };

        var context = CreateTestContext();

        // Act
        var result = ProxyClassMapper.MapToTemplateModel((table, new List<string>()), context);
        var resultColumns = GetColumnsFromResult(result);

        // Assert
        Assert.Single(resultColumns);
        Assert.Equal("Account_1", resultColumns[0].SchemaName);
    }

    [Fact]
    public void MapToTemplateModel_WithNonConflictingColumnNames_KeepsOriginalNames()
    {
        // Arrange
        var table = new TableModel
        {
            SchemaName = "TestEntity",
            LogicalName = "testentity",
            DisplayName = "Test Entity",
            Description = "Test entity",
            EntityTypeCode = 10001,
            PrimaryNameAttribute = "name",
            PrimaryIdAttribute = "testentityid",
            IsIntersect = false,
            Columns = new ColumnModel[]
            {
                new StringColumnModel
                {
                    SchemaName = "Name",
                    LogicalName = "name1",
                    DisplayName = "Name 1",
                    MaxLength = 100,
                },
                new StringColumnModel
                {
                    SchemaName = "Description",
                    LogicalName = "description",
                    DisplayName = "Description",
                    MaxLength = 100,
                },
                new StringColumnModel
                {
                    SchemaName = "Value",
                    LogicalName = "value",
                    DisplayName = "Value",
                    MaxLength = 100,
                },
            },
            Relationships = new List<RelationshipModel>(),
        };

        var context = CreateTestContext();

        // Act
        var result = ProxyClassMapper.MapToTemplateModel((table, new List<string>()), context);
        var resultColumns = GetColumnsFromResult(result);

        // Assert
        Assert.Equal(3, resultColumns.Count);

        // All columns should keep their original names since none conflict with class name
        Assert.Contains(resultColumns, c => c.SchemaName == "Name");
        Assert.Contains(resultColumns, c => c.SchemaName == "Description");
        Assert.Contains(resultColumns, c => c.SchemaName == "Value");
    }

    [Fact]
    public void MapToTemplateModel_WithCaseSensitiveClassNameConflict_AppliesRenaming()
    {
        // Arrange
        var table = new TableModel
        {
            SchemaName = "TestEntity",
            LogicalName = "testentity",
            DisplayName = "Test Entity",
            Description = "Test entity",
            EntityTypeCode = 10001,
            PrimaryNameAttribute = "name",
            PrimaryIdAttribute = "testentityid",
            IsIntersect = false,
            Columns = new ColumnModel[]
            {
                new StringColumnModel
                {
                    SchemaName = "TestEntity", // Exact match with class name
                    LogicalName = "testentity_field",
                    DisplayName = "TestEntity Field",
                    MaxLength = 100,
                },
                new StringColumnModel
                {
                    SchemaName = "testentity", // Different case - no conflict
                    LogicalName = "testentity_lower",
                    DisplayName = "testentity Lower",
                    MaxLength = 100,
                },
            },
            Relationships = new List<RelationshipModel>(),
        };

        var context = CreateTestContext();

        // Act
        var result = ProxyClassMapper.MapToTemplateModel((table, new List<string>()), context);
        var resultColumns = GetColumnsFromResult(result);

        // Assert
        Assert.Equal(2, resultColumns.Count);

        // Exact case match should get renamed
        Assert.Contains(resultColumns, c => c.SchemaName == "TestEntity_1");

        // Different case should keep original name (case-sensitive comparison)
        Assert.Contains(resultColumns, c => c.SchemaName == "testentity");
    }

    private static GenerationContext CreateTestContext()
    {
        return new GenerationContext
        {
            Templates = new EmbeddedTemplateProvider(),
            Namespace = "TestNamespace",
            Version = "1.0.0",
        };
    }

    private static List<ColumnModel> GetColumnsFromResult(object result)
    {
        var columnsProperty = result.GetType().GetProperty("Columns");
        var columns = (IEnumerable<ColumnModel>)columnsProperty!.GetValue(result)!;
        return columns.ToList();
    }
}