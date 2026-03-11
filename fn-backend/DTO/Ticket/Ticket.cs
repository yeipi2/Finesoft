namespace fs_backend.DTO;

public class TicketDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ⭐ CAMBIO: ProjectId ahora es NULLABLE
    // - Cliente: puede enviarlo como null
    // - Empleado/Admin: debe enviarlo obligatoriamente
    public int? ProjectId { get; set; }

    // ServiceId opcional
    public int? ServiceId { get; set; }

    public string Status { get; set; } = "Abierto";
    public string Priority { get; set; } = "Media";

    public string? AssignedToUserId { get; set; }

    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
}