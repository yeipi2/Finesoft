namespace fs_backend.DTO;

public class TicketStatsDto
{
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int InReview { get; set; }
    public int Closed { get; set; }
    public int Total { get; set; }
    public decimal TotalEstimatedHours { get; set; }
    public decimal TotalActualHours { get; set; }
}