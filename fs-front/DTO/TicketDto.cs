using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class TicketDto
{
    public int Id { get; set; } // Cambiado de nullable a int normal

    [Required(ErrorMessage = "El título es obligatorio")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria")]
    public string Description { get; set; } = string.Empty;

    // AGREGAR ProjectId (que faltaba)
    [Required(ErrorMessage = "El proyecto es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un proyecto válido")]
    public int ProjectId { get; set; }

    // ServiceId ahora es opcional (ya no lo usas)
    public int ServiceId { get; set; } = 0;

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