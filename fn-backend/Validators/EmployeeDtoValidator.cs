using FluentValidation;
using fn_backend.DTO;

namespace fs_backend.Validators;

public class EmployeeDtoValidator : AbstractValidator<EmployeeDto>
{
    public EmployeeDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
            .WithMessage("La contraseña debe tener al menos una mayúscula, una minúscula y un número")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("El rol es obligatorio")
            .Must(x => x == "Empleado" || x == "Supervisor" || x == "Administracion")
            .WithMessage("Rol inválido. Debe ser: Empleado, Supervisor o Administracion");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .Matches(@"^[a-zA-Z\s\-ñÑáéíóúÁÉÍÓÚ]+$")
            .WithMessage("El nombre contiene caracteres inválidos");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-()]*$").WithMessage("Teléfono inválido")
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("El puesto es obligatorio")
            .MaximumLength(100).WithMessage("El puesto no puede exceder 100 caracteres");

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("El departamento no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Department));

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("La fecha de contratación es obligatoria")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1)).WithMessage("La fecha de contratación no puede ser futura");
    }
}
