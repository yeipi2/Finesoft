namespace fs_front.DTO;

public class ClientReportDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TotalProjects { get; set; }
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingAmount { get; set; }
}