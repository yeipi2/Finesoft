using FluentValidation;
using fn_backend.DTO;

namespace fs_backend.Validators;

public class ClientDtoValidator : AbstractValidator<ClientDto>
{
    public ClientDtoValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("El nombre de la compañía es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .Matches(@"^[a-zA-Z0-9\s\.\-ñÑáéíóúÁÉÍÓÚ]+$")
            .WithMessage("El nombre de compañía contiene caracteres inválidos");

        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("El nombre de contacto es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .Matches(@"^[a-zA-Z\s\-ñÑáéíóúÁÉÍÓÚ]+$")
            .WithMessage("El nombre de contacto contiene caracteres inválidos");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es obligatorio")
            .Matches(@"^\+?[0-9\s\-()]+$").WithMessage("Teléfono inválido")
            .MinimumLength(10).WithMessage("El teléfono debe tener al menos 10 dígitos")
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres");

        RuleFor(x => x.RFC)
            .NotEmpty().WithMessage("El RFC es obligatorio")
            .Length(12, 13).WithMessage("RFC debe tener 12-13 caracteres")
            .Matches(@"^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$")
            .WithMessage("RFC inválido formato");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("La dirección es obligatoria")
            .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres");

        RuleFor(x => x.ServiceMode)
            .NotEmpty().WithMessage("El modo de servicio es obligatorio")
            .Must(x => x == "Mensual" || x == "Por Evento" || x == "Por evento")
            .WithMessage("Modo de servicio inválido");

        RuleFor(x => x.MonthlyRate)
            .GreaterThanOrEqualTo(0).WithMessage("La tarifa debe ser mayor o igual a 0")
            .When(x => x.MonthlyRate.HasValue);

        RuleFor(x => x.MonthlyHours)
            .GreaterThanOrEqualTo(0).WithMessage("Las horas mensuales deben ser mayor o igual a 0");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}
