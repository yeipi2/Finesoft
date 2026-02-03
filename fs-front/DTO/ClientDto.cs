using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class ClientDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El nombre de la compañía es obligatorio")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de contacto es obligatorio")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Phone(ErrorMessage = "Teléfono inválido")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RFC es obligatorio")]
    public string RFC { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modo de servicio es obligatorio")]
    public string ServiceMode { get; set; } = string.Empty;

    public double? MonthlyRate { get; set; }

    public bool IsActive { get; set; } = true;
}