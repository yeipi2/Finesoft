using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class ClientDto
{
    public int? Id { get; set; }

    // ⭐ Datos del Usuario
    public string? UserId { get; set; }

    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un número")]
    public string? Password { get; set; }

    // Datos del Cliente
    [Required(ErrorMessage = "El nombre de la compañía es obligatorio")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de contacto es obligatorio")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [RegularExpression(@"^\+?[0-9]{10,15}$",
        ErrorMessage = "Teléfono inválido. Debe contener entre 10 y 15 dígitos")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RFC es obligatorio")]
    [StringLength(13, MinimumLength = 12, ErrorMessage = "RFC debe tener 12-13 caracteres")]
    public string RFC { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria")]
    [StringLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modo de servicio es obligatorio")]
    public string ServiceMode { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "La tarifa debe ser mayor a 0")]
    public double? MonthlyRate { get; set; }

    public bool IsActive { get; set; } = true;
}