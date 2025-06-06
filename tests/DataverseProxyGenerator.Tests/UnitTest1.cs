using System.Collections.Generic;
using DataverseProxyGenerator.Core.Domain;
using DataverseProxyGenerator.Core.Generation;
using Xunit;

namespace DataverseProxyGenerator.Tests
{
    public class CodeGeneratorTests
    {
        [Fact]
        public void Generates_Correct_Code_For_String_Columns()
        {
            // Arrange
            var table = new TableModel
            {
                LogicalName = "account",
                DisplayName = "Account",
                Columns = new List<ColumnModel>
                {
                    new StringColumnModel { LogicalName = "name", SchemaName = "Name", DisplayName = "Account Name" },
                    new StringColumnModel { LogicalName = "description", SchemaName = "Description", DisplayName = "Account Description" }
                }
            };

            var generator = new CSharpProxyGenerator();

            // Act
            var files = generator.GenerateCode([table], "TestNamespace");

            // Assert
            var file = Assert.Single(files);
            Assert.Contains("public string Name", file.Content);
            Assert.Contains("public string Description", file.Content);
        }

        [Fact]
        public void ProxyClassTemplate_Generates_No_Extra_Newline_After_Last_Attribute()
        {
            // Arrange
            var table = new TableModel
            {
                SchemaName = "TestEntity",
                LogicalName = "testentity",
                DisplayName = "Test Entity",
                Columns = new List<ColumnModel>
                {
                    new StringColumnModel { LogicalName = "name", SchemaName = "Name", DisplayName = "Name" },
                    new IntegerColumnModel { LogicalName = "age", SchemaName = "Age", DisplayName = "Age", IsNullable = true }
                }
            };

            var generator = new CSharpProxyGenerator();

            // Act
            var files = generator.GenerateCode([table], "TestNamespace");

            // Assert
            var file = Assert.Single(files);
            var content = file.Content;

            // Check for correct property generation
            Assert.Contains("public string Name", content);
            Assert.Contains("public int? Age", content);

            // Check that there is NOT a double newline after the last attribute property
            var classBody = content.Substring(content.IndexOf("public partial class"));
            var lastPropertyEnd = classBody.LastIndexOf("}");
            var afterLastProperty = classBody.Substring(lastPropertyEnd, classBody.Length - lastPropertyEnd);
            Assert.DoesNotContain("\n\n\n", afterLastProperty); // No triple newline (double blank line)
        }

        [Fact]
        public void Generator_Emits_ObsoleteAttribute_When_IsObsolete_Is_True()
        {
            // Arrange
            var table = new TableModel
            {
                SchemaName = "ObsoleteTestEntity",
                LogicalName = "obsoletetestentity",
                DisplayName = "Obsolete Test Entity",
                Columns = new List<ColumnModel>
                {
                    new StringColumnModel { LogicalName = "oldfield", SchemaName = "OldField", DisplayName = "Old Field", IsObsolete = true },
                    new StringColumnModel { LogicalName = "newfield", SchemaName = "NewField", DisplayName = "New Field", IsObsolete = false }
                }
            };

            var generator = new CSharpProxyGenerator();

            // Act
            var files = generator.GenerateCode([table], "TestNamespace");

            // Assert
            var file = Assert.Single(files);
            var content = file.Content;

            // The obsolete property should have [ObsoleteAttribute()]
            Assert.Contains("[ObsoleteAttribute()]", content);
            // The non-obsolete property should not have [ObsoleteAttribute()] above it
            var obsoleteIndex = content.IndexOf("[ObsoleteAttribute()]");
            var oldFieldIndex = content.IndexOf("public string OldField");
            var newFieldIndex = content.IndexOf("public string NewField");

            Assert.True(obsoleteIndex > -1 && oldFieldIndex > obsoleteIndex, "ObsoleteAttribute should appear above OldField");
            // Ensure [ObsoleteAttribute()] does not appear above NewField
            var newFieldSection = content.Substring(newFieldIndex - 100 > 0 ? newFieldIndex - 100 : 0, 100);
            Assert.DoesNotContain("[ObsoleteAttribute()]", newFieldSection);
        }
    }
}
