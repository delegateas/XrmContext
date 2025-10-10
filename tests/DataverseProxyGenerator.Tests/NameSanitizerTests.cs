using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Tests;

public class NameSanitizerTests
{
    [Fact]
    public void SanitizeEnumOptionName_WithPlusCharacter_RemovesCharacter()
    {
        // Arrange
        var label = "Løbende måned + 14 dage";
        var optionValue = 11;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("Løbendemåned14dage", result);
    }

    [Fact]
    public void SanitizeEnumOptionName_WithNewlineCharacters_RemovesCharacters()
    {
        // Arrange
        var label = "Line1\nLine2\r\nLine3";
        var optionValue = 1;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("Line1Line2Line3", result);
    }

    [Fact]
    public void SanitizeEnumOptionName_WithTabCharacter_RemovesCharacter()
    {
        // Arrange
        var label = "Field1\tField2";
        var optionValue = 2;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("Field1Field2", result);
    }

    [Fact]
    public void SanitizeEnumOptionName_WithMultipleSpecialCharacters_RemovesAllCharacters()
    {
        // Arrange
        var label = "Test+Value\nWith\tSpecial.Characters";
        var optionValue = 3;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("TestValueWithSpecialCharacters", result);
    }

    [Fact]
    public void SanitizeName_WithPlusCharacter_RemovesCharacter()
    {
        // Arrange
        var name = "Field+Name";

        // Act
        var result = NameSanitizer.SanitizeName(name);

        // Assert
        Assert.Equal("FieldName", result);
    }

    [Fact]
    public void SanitizeName_WithNewlineCharacters_RemovesCharacters()
    {
        // Arrange
        var name = "Multi\nLine\r\nText";

        // Act
        var result = NameSanitizer.SanitizeName(name);

        // Assert
        Assert.Equal("MultiLineText", result);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\t")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("'")]
    [InlineData("-")]
    [InlineData("–")]
    [InlineData("%")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData(",")]
    [InlineData(":")]
    [InlineData(";")]
    [InlineData("/")]
    [InlineData("\\")]
    [InlineData("&")]
    public void SanitizeName_WithSpecialCharacter_RemovesCharacter(string specialChar)
    {
        // Arrange
        var name = $"Test{specialChar}Value";

        // Act
        var result = NameSanitizer.SanitizeName(name);

        // Assert
        Assert.Equal($"TestValue", result);
    }
}