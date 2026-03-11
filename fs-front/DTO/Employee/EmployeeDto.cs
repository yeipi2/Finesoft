using System.ComponentModel.DataAnnotations;
namespace fs_front.DTO;

public class EmployeeDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "Debe tener 8+ caracteres, una MAYÚSCULA, una minúscula, un número y un símbolo")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio")]
    public string RoleName { get; set; } = string.Empty;
    public string? UserId { get; set; }

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Teléfono inválido")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "El puesto es obligatorio")]
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime HireDate { get; set; } = DateTime.Today;
}