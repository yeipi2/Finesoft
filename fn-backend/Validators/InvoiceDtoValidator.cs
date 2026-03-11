using FluentValidation;
using fs_backend.DTO;

namespace fs_backend.Validators;

public class InvoiceItemDtoValidator : AbstractValidator<InvoiceItemDto>
{
    public InvoiceItemDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo");

        RuleFor(x => x.ServiceId)
            .GreaterThan(0).WithMessage("Debe seleccionar un servicio válido")
            .When(x => x.ServiceId.HasValue);

        RuleFor(x => x.TicketId)
            .GreaterThan(0).WithMessage("Debe seleccionar un ticket válido")
            .When(x => x.TicketId.HasValue);
    }
}

public class InvoiceDtoValidator : AbstractValidator<InvoiceDto>
{
    public InvoiceDtoValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0).WithMessage("Debe seleccionar un cliente válido");

        RuleFor(x => x.QuoteId)
            .GreaterThan(0).WithMessage("Debe seleccionar una cotización válida")
            .When(x => x.QuoteId.HasValue);

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("La fecha de factura es obligatoria")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1)).WithMessage("La fecha de factura no puede ser futura");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.InvoiceDate ?? DateTime.Today)
            .WithMessage("La fecha de vencimiento debe ser mayor o igual a la fecha de factura")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.InvoiceType)
            .NotEmpty().WithMessage("El tipo de factura es obligatorio")
            .Must(x => x == "Event" || x == "Monthly")
            .WithMessage("Tipo de factura inválido. Debe ser: Event o Monthly");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es obligatorio")
            .Must(x => x == "Pendiente" || x == "Pagada" || x == "Vencida" || x == "Cancelada")
            .WithMessage("Estado de factura inválido");

        RuleFor(x => x.PaymentType)
            .NotEmpty().WithMessage("El tipo de pago es obligatorio")
            .Must(x => x == "PUE" || x == "PPD")
            .WithMessage("Tipo de pago inválido. Debe ser: PUE o PPD");

        RuleFor(x => x.PaymentMethod)
            .MaximumLength(50).WithMessage("El método de pago no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.PaymentMethod));

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Las notas no pueden exceder 2000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Items)
            .NotNull().WithMessage("La factura debe tener items")
            .NotEmpty().WithMessage("La factura debe tener al menos un elemento");

        RuleForEach(x => x.Items)
            .SetValidator(new InvoiceItemDtoValidator());
    }
}
