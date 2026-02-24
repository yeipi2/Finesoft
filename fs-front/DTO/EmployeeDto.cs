using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class EmployeeDto
{
    public int? Id { get; set; }

    // Datos del Usuario
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio")]
    public string RoleName { get; set; } = string.Empty;

    public string? UserId { get; set; }

    // Datos Personales
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Teléfono inválido")]
    public string Phone { get; set; } = string.Empty;

    // Datos Laborales
    [Required(ErrorMessage = "El puesto es obligatorio")]
    public string Position { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime HireDate { get; set; } = DateTime.Today;
}