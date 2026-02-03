namespace fs_backend.DTO;

public class ProjectReportDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int ClosedTickets { get; set; }
    public decimal TotalHours { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal Revenue { get; set; }
}