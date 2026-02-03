namespace fs_backend.DTO;

public class RevenueTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int InvoicesCount { get; set; }
}