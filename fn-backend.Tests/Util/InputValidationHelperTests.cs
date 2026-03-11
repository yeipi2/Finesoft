using FluentAssertions;
using fs_backend.Util;

namespace fn_backend.Tests.Util;

public class InputValidationHelperTests
{
    [Fact]
    public void ValidateInput_NullInput_ShouldReturnValid()
    {
        var result = InputValidationHelper.ValidateInput(null, "TestField");
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateInput_WhiteSpaceInput_ShouldReturnValid()
    {
        var result = InputValidationHelper.ValidateInput("   ", "TestField");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateInput_ExceedMaxLength_ShouldReturnInvalid()
    {
        var input = new string('a', 501);
        var result = InputValidationHelper.ValidateInput(input, "TestField", 500);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("excede el límite");
    }

    [Fact]
    public void ValidateInput_ContainsDangerousChars_ShouldReturnInvalid()
    {
        var input = "<script>alert('xss')</script>";
        var result = InputValidationHelper.ValidateInput(input, "TestField", allowHtml: false);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("caracteres no permitidos");
    }

    [Fact]
    public void ValidateInput_ContainsSqlInjection_ShouldReturnInvalid()
    {
        var input = "SELECT * FROM Users";
        var result = InputValidationHelper.ValidateInput(input, "TestField");
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("patrones potencialmente");
    }

    [Fact]
    public void ValidateInput_ValidInput_ShouldReturnValid()
    {
        var input = "This is a valid input";
        var result = InputValidationHelper.ValidateInput(input, "TestField");
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateInput_WithHtmlAllowed_ShouldPass()
    {
        var input = "<p>Hello <b>World</b></p>";
        var result = InputValidationHelper.ValidateInput(input, "TestField", allowHtml: true);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateInput_CustomMaxLength_ShouldRespect()
    {
        var input = new string('a', 101);
        var result = InputValidationHelper.ValidateInput(input, "TestField", maxLength: 100);
        result.IsValid.Should().BeFalse();
    }
}
