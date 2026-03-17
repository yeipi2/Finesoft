using FluentValidation;
using fs_backend.DTO;

namespace fs_backend.Validators;

public class TicketDtoValidator : AbstractValidator<TicketDto>
{
    public TicketDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es obligatorio")
            .MaximumLength(200).WithMessage("El título no puede exceder 200 caracteres");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(5000).WithMessage("La descripción no puede exceder 5000 caracteres");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es obligatorio")
            .Must(x => x == "Abierto" || x == "En Progreso" || x == "En Revisión" || x == "Cerrado")
            .WithMessage("Estado de ticket inválido");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("La prioridad es obligatoria")
            .Must(x => x == "Baja" || x == "Media" || x == "Alta" || x == "Urgente")
            .WithMessage("Prioridad inválida");

        // Las horas estimadas son opcionales para clientes que crean tickets básicos
        // Se permite null, 0 o cualquier valor positivo
        RuleFor(x => x.EstimatedHours)
            .Must(x => x == null || x >= 0)
            .WithMessage("Las horas estimadas no pueden ser negativas");

        RuleFor(x => x.EstimatedHours)
            .LessThanOrEqualTo(1000).WithMessage("Las horas estimadas no pueden exceder 1000")
            .When(x => x.EstimatedHours.HasValue && x.EstimatedHours > 0);
    }
}

public class TicketCommentDtoValidator : AbstractValidator<TicketCommentDto>
{
    public TicketCommentDtoValidator()
    {
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("El comentario no puede estar vacío")
            .MaximumLength(5000).WithMessage("El comentario no puede exceder 5000 caracteres");
    }
}
