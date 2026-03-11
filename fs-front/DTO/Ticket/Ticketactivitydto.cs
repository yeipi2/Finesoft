using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class TicketActivityDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }

    [Required(ErrorMessage = "La descripción es requerida")]
    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Las horas son requeridas")]
    [Range(0.1, 999, ErrorMessage = "Las horas deben estar entre 0.1 y 999")]
    public decimal HoursSpent { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
}