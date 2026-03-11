using FluentValidation;
using fn_backend.DTO;

namespace fs_backend.Validators;

public class ProjectDtoValidator : AbstractValidator<ProjectDto>
{
    public ProjectDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del proyecto es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .Matches(@"^[a-zA-Z0-9\s\.\-ñÑáéíóúÁÉÍÓÚ]+$")
            .WithMessage("El nombre contiene caracteres inválidos");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ClientId)
            .GreaterThan(0).WithMessage("Debe seleccionar un cliente válido");
    }
}
