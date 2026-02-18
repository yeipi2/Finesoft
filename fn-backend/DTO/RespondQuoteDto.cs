using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class RespondQuoteDto
{
    [Required]
    [RegularExpression("^(Aceptada|Rechazada)$", ErrorMessage = "El estado debe ser 'Aceptada' o 'Rechazada'")]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Comments { get; set; }
}