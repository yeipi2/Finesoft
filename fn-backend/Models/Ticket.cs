using fn_backend.Models;

namespace fs_backend.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // RELACIÓN DIRECTA CON PROYECTO (nuevo)
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    // ServiceId ahora es nullable y opcional
    public int? ServiceId { get; set; }

    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
    public ICollection<TicketActivity> Activities { get; set; } = new List<TicketActivity>();
}