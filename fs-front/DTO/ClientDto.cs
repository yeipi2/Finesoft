using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class ClientDto
{
    public int? Id { get; set; }

    public string? UserId { get; set; }

    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Debe tener 8+ caracteres, una MAYÚSCULA, una minúscula y un número")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El nombre de la compañía es obligatorio")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de contacto es obligatorio")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [RegularExpression(@"^\+?[0-9]{10,15}$",
        ErrorMessage = "Debe contener entre 10 y 15 dígitos")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RFC es obligatorio")]
    [StringLength(13, MinimumLength = 12, ErrorMessage = "RFC debe tener 12-13 caracteres")]
    public string RFC { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modo de servicio es obligatorio")]
    public string ServiceMode { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Debe ser mayor a 0")]
    public double? MonthlyRate { get; set; }

    public bool IsActive { get; set; } = true;

    public int ProjectCount { get; set; } = 0;
}