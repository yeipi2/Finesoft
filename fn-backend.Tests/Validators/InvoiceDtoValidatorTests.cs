using FluentAssertions;
using fn_backend.DTO;
using fs_backend.Validators;

namespace fn_backend.Tests.Validators;

public class InvoiceDtoValidatorTests
{
    private readonly InvoiceDtoValidator _validator;

    public InvoiceDtoValidatorTests()
    {
        _validator = new InvoiceDtoValidator();
    }

    private InvoiceDto CreateValidInvoice()
    {
        return new InvoiceDto
        {
            ClientId = 1,
            QuoteId = 1,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            InvoiceType = "Event",
            Status = "Pendiente",
            PaymentType = "PUE",
            PaymentMethod = "Transferencia",
            Notes = "Notas de prueba",
            Items = new List<InvoiceItemDto>
            {
                new InvoiceItemDto
                {
                    Description = "Servicio de desarrollo",
                    Quantity = 10,
                    UnitPrice = 150,
                    ServiceId = 1
                }
            }
        };
    }

    [Fact]
    public void Validate_ValidInvoice_ShouldPass()
    {
        var invoice = CreateValidInvoice();
        var result = _validator.TestValidate(invoice);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroClientId_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.ClientId = 0;
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Fact]
    public void Validate_FutureInvoiceDate_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.InvoiceDate = DateTime.Today.AddDays(1);
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor(x => x.InvoiceDate);
    }

    [Fact]
    public void Validate_DueDateBeforeInvoiceDate_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.DueDate = invoice.InvoiceDate?.AddDays(-1);
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Theory]
    [InlineData("Event")]
    [InlineData("Monthly")]
    public void Validate_ValidInvoiceType_ShouldPass(string type)
    {
        var invoice = CreateValidInvoice();
        invoice.InvoiceType = type;
        var result = _validator.TestValidate(invoice);
        result.ShouldNotHaveValidationErrorFor(x => x.InvoiceType);
    }

    [Fact]
    public void Validate_InvalidInvoiceType_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.InvoiceType = "InvalidType";
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor(x => x.InvoiceType);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Pagada")]
    [InlineData("Vencida")]
    [InlineData("Cancelada")]
    public void Validate_ValidStatus_ShouldPass(string status)
    {
        var invoice = CreateValidInvoice();
        invoice.Status = status;
        var result = _validator.TestValidate(invoice);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_InvalidStatus_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.Status = "InvalidStatus";
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("PUE")]
    [InlineData("PPD")]
    public void Validate_ValidPaymentType_ShouldPass(string paymentType)
    {
        var invoice = CreateValidInvoice();
        invoice.PaymentType = paymentType;
        var result = _validator.TestValidate(invoice);
        result.ShouldNotHaveValidationErrorFor(x => x.PaymentType);
    }

    [Fact]
    public void Validate_InvalidPaymentType_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.PaymentType = "INVALID";
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor(x => x.PaymentType);
    }

    [Fact]
    public void Validate_EmptyItems_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItemDto>();
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_EmptyItemDescription_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItemDto>
        {
            new InvoiceItemDto { Description = "", Quantity = 1, UnitPrice = 100 }
        };
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor("Items[0].Description");
    }

    [Fact]
    public void Validate_ZeroQuantity_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItemDto>
        {
            new InvoiceItemDto { Description = "Test", Quantity = 0, UnitPrice = 100 }
        };
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Validate_NegativeUnitPrice_ShouldFail()
    {
        var invoice = CreateValidInvoice();
        invoice.Items = new List<InvoiceItemDto>
        {
            new InvoiceItemDto { Description = "Test", Quantity = 1, UnitPrice = -10 }
        };
        var result = _validator.TestValidate(invoice);
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
    }
}
