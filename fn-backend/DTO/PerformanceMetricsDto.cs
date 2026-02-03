namespace fs_backend.DTO;

public class PerformanceMetricsDto
{
    public decimal TicketResolutionRate { get; set; }
    public decimal AverageResolutionTime { get; set; }
    public decimal ClientSatisfactionScore { get; set; }
    public decimal BillingEfficiency { get; set; }
    public decimal ResourceUtilization { get; set; }
    public int TotalTicketsResolved { get; set; }
    public int TotalTicketsCreated { get; set; }
}