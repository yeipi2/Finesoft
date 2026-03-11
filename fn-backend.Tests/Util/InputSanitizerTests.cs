using FluentAssertions;
using fs_backend.Util;

namespace fn_backend.Tests.Util;

public class InputSanitizerTests
{
    [Fact]
    public void Sanitize_NullInput_ShouldReturnEmptyString()
    {
        var result = InputSanitizer.Sanitize(null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_WhiteSpaceInput_ShouldReturnEmptyString()
    {
        var result = InputSanitizer.Sanitize("   ");
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_ValidInput_ShouldReturnTrimmedInput()
    {
        var result = InputSanitizer.Sanitize("  Hello World  ");
        result.Should().Be("Hello World");
    }

    [Fact]
    public void Sanitize_InputWithHtmlTags_ShouldEncodeHtml()
    {
        var result = InputSanitizer.Sanitize("<script>alert('xss')</script>");
        result.Should().NotContain("<script>");
    }

    [Fact]
    public void SanitizeHtml_NullInput_ShouldReturnEmptyString()
    {
        var result = InputSanitizer.SanitizeHtml(null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeHtml_InputWithHtmlTags_ShouldRemoveTags()
    {
        var result = InputSanitizer.SanitizeHtml("<p>Hello</p><script>alert('xss')</script>");
        result.Should().Be("Hello");
    }

    [Theory]
    [InlineData("SELECT * FROM Users")]
    [InlineData("INSERT INTO Users")]
    [InlineData("DROP TABLE Users")]
    [InlineData("DELETE FROM Users")]
    [InlineData("UNION SELECT password")]
    public void ContainsSqlInjection_SqlKeywords_ShouldReturnTrue(string input)
    {
        var result = InputSanitizer.ContainsSqlInjection(input);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("SELECT")]
    [InlineData("INSERT")]
    [InlineData("UPDATE")]
    [InlineData("DELETE")]
    [InlineData("DROP")]
    public void ContainsSqlInjection_StandaloneSqlKeywords_ShouldReturnTrue(string keyword)
    {
        var result = InputSanitizer.ContainsSqlInjection(keyword);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Hello World")]
    [InlineData("This is a normal text")]
    public void ContainsSqlInjection_ValidInput_ShouldReturnFalse(string input)
    {
        var result = InputSanitizer.ContainsSqlInjection(input);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("SELECT * FROM Users WHERE name = '")]
    public void ContainsDangerousChars_MaliciousInput_ShouldReturnTrue(string input)
    {
        var result = InputSanitizer.ContainsDangerousChars(input);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Normal text without special chars")]
    public void ContainsDangerousChars_ValidInput_ShouldReturnFalse(string input)
    {
        var result = InputSanitizer.ContainsDangerousChars(input);
        result.Should().BeFalse();
    }

    [Fact]
    public void SanitizeForDatabase_NullInput_ShouldReturnEmptyString()
    {
        var result = InputSanitizer.SanitizeForDatabase(null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForDatabase_InputWithSqlInjection_ShouldSanitize()
    {
        var result = InputSanitizer.SanitizeForDatabase("'; DROP TABLE Users; --");
        result.Should().NotContain("DROP");
        result.Should().NotContain(";");
    }

    [Fact]
    public void SanitizeFileName_NullFileName_ShouldReturnDefault()
    {
        var result = InputSanitizer.SanitizeFileName(null);
        result.Should().Be("file");
    }

    [Fact]
    public void SanitizeFileName_FileNameWithInvalidChars_ShouldRemoveInvalid()
    {
        var result = InputSanitizer.SanitizeFileName("file<>:\"/\\|?*.txt");
        result.Should().NotContain("<");
        result.Should().NotContain(">");
        result.Should().NotContain(":");
    }

    [Fact]
    public void SanitizeFileName_ValidFileName_ShouldReturnSame()
    {
        var result = InputSanitizer.SanitizeFileName("documento_valido.pdf");
        result.Should().Be("documento_valido");
    }
}
