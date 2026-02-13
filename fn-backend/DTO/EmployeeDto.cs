using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class EmployeeDto
{
    public int? Id { get; set; }

    // Datos del Usuario (para crear usuario junto con empleado)
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un número")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio")]
    public string RoleName { get; set; } = string.Empty;

    public string? UserId { get; set; } // Para edición, viene del backend

    // Datos del Empleado
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RFC es obligatorio")]
    [StringLength(13, MinimumLength = 12, ErrorMessage = "RFC debe tener 12-13 caracteres")]
    public string RFC { get; set; } = string.Empty;

    [StringLength(18, MinimumLength = 18, ErrorMessage = "CURP debe tener 18 caracteres")]
    public string CURP { get; set; } = string.Empty;

    [Required(ErrorMessage = "El puesto es obligatorio")]
    public string Position { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Teléfono inválido")]
    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de contratación es obligatoria")]
    public DateTime HireDate { get; set; } = DateTime.Today;

    [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser positivo")]
    public decimal? Salary { get; set; }

    public bool IsActive { get; set; } = true;

    public string EmergencyContactName { get; set; } = string.Empty;

    [Phone]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}