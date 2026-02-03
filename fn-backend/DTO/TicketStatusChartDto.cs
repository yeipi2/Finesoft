namespace fs_backend.DTO;

public class TicketStatusChartDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Color { get; set; } = string.Empty;
}