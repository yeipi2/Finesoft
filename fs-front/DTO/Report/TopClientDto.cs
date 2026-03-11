namespace fs_front.DTO;

public class TopClientDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int InvoicesCount { get; set; }
}