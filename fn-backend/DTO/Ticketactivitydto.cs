namespace fs_backend.DTO;

public class TicketActivityDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal HoursSpent { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
}