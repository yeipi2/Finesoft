namespace fs_backend.DTO;

public class DashboardStatsDto
{
    public int TotalClients { get; set; }
    public int ActiveProjects { get; set; }
    public int OpenTickets { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal PendingPayments { get; set; }
    public int TotalInvoices { get; set; }
    public int TotalQuotes { get; set; }
}