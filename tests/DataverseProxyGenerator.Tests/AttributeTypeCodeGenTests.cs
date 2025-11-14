using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;

namespace DataverseProxyGenerator.Tests;

public class AttributeTypeCodeGenTests
{
    [Fact]
    public async Task Generates_Correct_Code_For_All_AttributeTypes()
    {
        var table = new TableModel
        {
            SchemaName = "TestEntity",
            LogicalName = "testentity",
            DisplayName = "Test Entity",
            Columns = new List<ColumnModel>
            {
                new ManagedColumnModel("string", IsNullable: false) { LogicalName = "readonlyattribute", SchemaName = "ReadOnlyAttribute", DisplayName = "A ReadOnly Attribute" },
                new BooleanManagedColumnModel { LogicalName = "managedbooleanattribute", SchemaName = "ManagedBooleanAttribute", DisplayName = "A Managed Boolean Attribute" },
                new ManagedColumnModel("DateTime", IsNullable: true) { LogicalName = "manageddatetimeattribute", SchemaName = "ManagedDateTimeAttribute", DisplayName = "A Managed DateTime Attribute" },
                new StringColumnModel { LogicalName = "obsoleteattribute", SchemaName = "ObsoleteAttribute", DisplayName = "An Obsolete Attribute", IsObsolete = true },
                new StringColumnModel { LogicalName = "name", SchemaName = "Name", DisplayName = "Name" },
                new StringColumnModel { LogicalName = "prefix_pascalcasetest_withname", SchemaName = "prefix_pascalCaseTest_withName", DisplayName = "Pascal Test" },
                new IntegerColumnModel { LogicalName = "age", SchemaName = "Age", DisplayName = "Age" },
                new IntegerColumnModel { LogicalName = "score", SchemaName = "Score", DisplayName = "Score" },
                new BooleanColumnModel { LogicalName = "isactive", SchemaName = "IsActive", DisplayName = "Is Active" },
                new DecimalColumnModel { LogicalName = "amount", SchemaName = "Amount", DisplayName = "Amount", Precision = 2 },
                new DoubleColumnModel { LogicalName = "ratio", SchemaName = "Ratio", DisplayName = "Ratio" },
                new MoneyColumnModel { LogicalName = "revenue", SchemaName = "Revenue", DisplayName = "Revenue" },
                new DateTimeColumnModel { LogicalName = "createdon", SchemaName = "CreatedOn", DisplayName = "Created On" },
                new PrimaryIdColumnModel { LogicalName = "id", SchemaName = "Id", DisplayName = "Id" },
                new MemoColumnModel { LogicalName = "notes", SchemaName = "Notes", DisplayName = "Notes" },
                new FileColumnModel { LogicalName = "document", SchemaName = "Document", DisplayName = "Document" },
                new ImageColumnModel { LogicalName = "profilepic", SchemaName = "ProfilePic", DisplayName = "Profile Picture" },
                new EnumColumnModel
                {
                    LogicalName = "status",
                    SchemaName = "Status",
                    DisplayName = "Status",
                    OptionsetName = "StatusSet",
                    IsGlobalOptionset = false,
                    IsMultiSelect = false,
                    OptionsetValues = new Dictionary<int, string> { { 1, "Active" }, { 2, "Inactive" } },
                    OptionLocalizations = new Dictionary<int, Dictionary<int, string>>(),
                },
                new LookupColumnModel
                {
                    LogicalName = "contactid",
                    SchemaName = "ContactId",
                    DisplayName = "Contact",
                    TargetTable = "Contact",
                    RelationshipName = "contact_account",
                },
                new PartyListColumnModel { LogicalName = "participants", SchemaName = "Participants", DisplayName = "Participants" },
                new UniqueIdentifierColumnModel { LogicalName = "uniqueid", SchemaName = "UniqueId", DisplayName = "Unique Identifier" },
            },
        };

        var generator = new CSharpProxyGenerator();
        var files = generator.GenerateCode(
            new[] { table },
            Enumerable.Empty<CustomApiModel>(),
            new XrmGenerationConfig("Output", "TestNamespace", "TestContextName", new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture).AsReadOnly()));
        var file = files.FirstOrDefault(f => f.Filename.EndsWith("TestEntity.cs", StringComparison.InvariantCulture));

        Assert.NotNull(file);
        await Verify(file.Content);
    }
}