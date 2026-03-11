using FluentValidation;
using fs_backend.DTO;

namespace fs_backend.Validators;

public class QuoteItemDtoValidator : AbstractValidator<QuoteItemDto>
{
    public QuoteItemDtoValidator()
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

public class QuoteDtoValidator : AbstractValidator<QuoteDto>
{
    public QuoteDtoValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0).WithMessage("Debe seleccionar un cliente válido");

        RuleFor(x => x.ValidUntil)
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("La fecha de validez debe ser hoy o posterior")
            .When(x => x.ValidUntil.HasValue);

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es obligatorio")
            .Must(x => x == "Borrador" || x == "Enviada" || x == "Aceptada" || x == "Rechazada" || x == "Vencida")
            .WithMessage("Estado de cotización inválido");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Las notas no pueden exceder 2000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Items)
            .NotNull().WithMessage("La cotización debe tener items")
            .NotEmpty().WithMessage("La cotización debe tener al menos un elemento");

        RuleForEach(x => x.Items)
            .SetValidator(new QuoteItemDtoValidator());
    }
}
