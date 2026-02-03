namespace fs_backend.Models;

public class TicketActivity
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal HoursSpent { get; set; }
    public bool IsCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
}