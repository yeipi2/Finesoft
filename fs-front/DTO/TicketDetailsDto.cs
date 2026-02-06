namespace fs_front.DTO;

public class TicketDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal HourlyRate { get; set; }

    public List<TicketCommentDto> Comments { get; set; } = new();
    public List<TicketAttachmentDto> Attachments { get; set; } = new();
    public List<TicketHistoryDto> History { get; set; } = new();

    // ⭐ ASEGÚRATE DE TENER ESTA PROPIEDAD
    public List<TicketActivityDto> Activities { get; set; } = new();
}