using FluentAssertions;
using fn_backend.DTO;
using fs_backend.Validators;

namespace fn_backend.Tests.Validators;

public class QuoteDtoValidatorTests
{
    private readonly QuoteDtoValidator _validator;

    public QuoteDtoValidatorTests()
    {
        _validator = new QuoteDtoValidator();
    }

    private QuoteDto CreateValidQuote()
    {
        return new QuoteDto
        {
            ClientId = 1,
            ValidUntil = DateTime.Today.AddDays(30),
            Status = "Borrador",
            Notes = "Notas de prueba",
            Items = new List<QuoteItemDto>
            {
                new QuoteItemDto
                {
                    Description = "Desarrollo de funcionalidad",
                    Quantity = 20,
                    UnitPrice = 500,
                    ServiceId = 1
                }
            }
        };
    }

    [Fact]
    public void Validate_ValidQuote_ShouldPass()
    {
        var quote = CreateValidQuote();
        var result = _validator.TestValidate(quote);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroClientId_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.ClientId = 0;
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Fact]
    public void Validate_PastValidUntilDate_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.ValidUntil = DateTime.Today.AddDays(-1);
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor(x => x.ValidUntil);
    }

    [Theory]
    [InlineData("Borrador")]
    [InlineData("Enviada")]
    [InlineData("Aceptada")]
    [InlineData("Rechazada")]
    [InlineData("Vencida")]
    public void Validate_ValidStatus_ShouldPass(string status)
    {
        var quote = CreateValidQuote();
        quote.Status = status;
        var result = _validator.TestValidate(quote);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_InvalidStatus_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.Status = "InvalidStatus";
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_EmptyItems_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.Items = new List<QuoteItemDto>();
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_EmptyItemDescription_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.Items = new List<QuoteItemDto>
        {
            new QuoteItemDto { Description = "", Quantity = 1, UnitPrice = 100 }
        };
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor("Items[0].Description");
    }

    [Fact]
    public void Validate_ZeroQuantity_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.Items = new List<QuoteItemDto>
        {
            new QuoteItemDto { Description = "Test", Quantity = 0, UnitPrice = 100 }
        };
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Validate_NegativeUnitPrice_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.Items = new List<QuoteItemDto>
        {
            new QuoteItemDto { Description = "Test", Quantity = 1, UnitPrice = -50 }
        };
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
    }

    [Fact]
    public void Validate_NotesExceedMaxLength_ShouldFail()
    {
        var quote = CreateValidQuote();
        quote.Notes = new string('a', 2001);
        var result = _validator.TestValidate(quote);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
