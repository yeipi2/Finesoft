using FluentValidation;
using fn_backend.DTO;

namespace fs_backend.Validators;

public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .MinimumLength(3).WithMessage("El usuario debe tener al menos 3 caracteres")
            .MaximumLength(50).WithMessage("El usuario no puede exceder 50 caracteres");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
            .WithMessage("La contraseña debe tener al menos una mayúscula, una minúscula y un número")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("El rol es obligatorio")
            .Must(x => x == "Admin" || x == "Administracion" || x == "Empleado" || x == "Supervisor" || x == "Cliente")
            .WithMessage("Rol inválido");
    }
}

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
            .WithMessage("La contraseña debe tener al menos una mayúscula, una minúscula y un número");
    }
}

public class ProfileUpdateDtoValidator : AbstractValidator<ProfileUpdateDto>
{
    public ProfileUpdateDtoValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .Matches(@"^[a-zA-Z\s\-ñÑáéíóúÁÉÍÓÚ]*$")
            .WithMessage("El nombre contiene caracteres inválidos")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-()]*$").WithMessage("Teléfono inválido")
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}
