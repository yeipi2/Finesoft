using FluentAssertions;
using FluentValidation;
using fn_backend.DTO;
using fs_backend.Validators;

namespace fn_backend.Tests.Validators;

public class ClientDtoValidatorTests
{
    private readonly ClientDtoValidator _validator;

    public ClientDtoValidatorTests()
    {
        _validator = new ClientDtoValidator();
    }

    private ClientDto CreateValidClient()
    {
        return new ClientDto
        {
            CompanyName = "Empresa Test",
            ContactName = "Juan Perez",
            Email = "test@empresa.com",
            Phone = "+525512345678",
            RFC = "XAXX010101000",
            Address = "Calle Test 123",
            ServiceMode = "Mensual",
            MonthlyRate = 1000,
            MonthlyHours = 20
        };
    }

    [Fact]
    public void Validate_ValidClient_ShouldPass()
    {
        var client = CreateValidClient();
        var result = _validator.Validate(client);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCompanyName_ShouldFail()
    {
        var client = CreateValidClient();
        client.CompanyName = "";
        var result = _validator.Validate(client);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_InvalidEmail_ShouldFail()
    {
        var client = CreateValidClient();
        client.Email = "not-an-email";
        var result = _validator.Validate(client);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_InvalidRFC_ShouldFail()
    {
        var client = CreateValidClient();
        client.RFC = "INVALID";
        var result = _validator.Validate(client);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_InvalidPhone_ShouldFail()
    {
        var client = CreateValidClient();
        client.Phone = "123";
        var result = _validator.Validate(client);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Mensual")]
    [InlineData("Por evento")]
    public void Validate_ValidServiceMode_ShouldPass(string serviceMode)
    {
        var client = CreateValidClient();
        client.ServiceMode = serviceMode;
        var result = _validator.Validate(client);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidServiceMode_ShouldFail()
    {
        var client = CreateValidClient();
        client.ServiceMode = "InvalidMode";
        var result = _validator.Validate(client);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeMonthlyRate_ShouldFail()
    {
        var client = CreateValidClient();
        client.MonthlyRate = -100;
        var result = _validator.Validate(client);
        result.IsValid.Should().BeFalse();
    }
}
