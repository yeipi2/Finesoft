namespace fs_backend.DTO;

public class UserReportDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TicketsCreated { get; set; }
    public int TicketsClosed { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal Revenue { get; set; }
}