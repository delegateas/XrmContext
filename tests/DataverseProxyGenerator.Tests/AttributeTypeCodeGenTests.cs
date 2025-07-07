using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;
using VerifyXunit;
using Xunit;

namespace DataverseProxyGenerator.Tests
{
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
                    new StringColumnModel { LogicalName = "obsoleteattribute", SchemaName = "ObsoleteAttribute", DisplayName = "An Obsolete Attribute", IsObsolete = true },
                    new StringColumnModel { LogicalName = "name", SchemaName = "Name", DisplayName = "Name" },
                    new IntegerColumnModel { LogicalName = "age", SchemaName = "Age", DisplayName = "Age", IsNullable = false },
                    new IntegerColumnModel { LogicalName = "score", SchemaName = "Score", DisplayName = "Score", IsNullable = true },
                    new BooleanColumnModel { LogicalName = "isactive", SchemaName = "IsActive", DisplayName = "Is Active" },
                    new DecimalColumnModel { LogicalName = "amount", SchemaName = "Amount", DisplayName = "Amount", Precision = 2 },
                    new DoubleColumnModel { LogicalName = "ratio", SchemaName = "Ratio", DisplayName = "Ratio" },
                    new MoneyColumnModel { LogicalName = "revenue", SchemaName = "Revenue", DisplayName = "Revenue" },
                    new DateTimeColumnModel { LogicalName = "createdon", SchemaName = "CreatedOn", DisplayName = "Created On" },
                    new PrimaryIdColumnModel { LogicalName = "id", SchemaName = "Id", DisplayName = "Id" },
                    new MemoColumnModel { LogicalName = "notes", SchemaName = "Notes", DisplayName = "Notes" },
                    new FileColumnModel { LogicalName = "document", SchemaName = "Document", DisplayName = "Document" },
                    new ImageColumnModel { LogicalName = "profilepic", SchemaName = "ProfilePic", DisplayName = "Profile Picture" },
                    new EnumColumnModel {
                        LogicalName = "status",
                        SchemaName = "Status",
                        DisplayName = "Status",
                        OptionsetName = "StatusSet",
                        IsGlobalOptionset = false,
                        IsMultiSelect = false,
                        OptionsetValues = new Dictionary<int, string> { { 1, "Active" }, { 2, "Inactive" } },
                        OptionLocalizations = new Dictionary<int, Dictionary<int, string>>()
                    },
                    new LookupColumnModel {
                        LogicalName = "contactid",
                        SchemaName = "ContactId",
                        DisplayName = "Contact",
                        TargetTable = "Contact",
                        RelationshipName = "contact_account"
                    },
                    new PartyListColumnModel { LogicalName = "participants", SchemaName = "Participants", DisplayName = "Participants" }
                }
            };

            var generator = new CSharpProxyGenerator();
            var files = generator.GenerateCode(new[] { table }, "TestNamespace", new Dictionary<string, List<string>>());
            var file = files.FirstOrDefault(f => f.Filename.EndsWith("TestEntity.cs"));

            Assert.NotNull(file);
            await Verifier.Verify(file.Content);
        }
    }
}
