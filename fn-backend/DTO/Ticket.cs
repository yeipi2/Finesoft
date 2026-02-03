namespace fs_backend.DTO;

using System.ComponentModel.DataAnnotations;

public class TicketDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "El servicio es obligatorio")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio")]
    public string Status { get; set; } = "Abierto";

    [Required(ErrorMessage = "La prioridad es obligatoria")]
    public string Priority { get; set; } = "Media";

    public string? AssignedToUserId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Las horas estimadas deben ser mayor o igual a 0")]
    public decimal EstimatedHours { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Las horas reales deben ser mayor o igual a 0")]
    public decimal ActualHours { get; set; }
}