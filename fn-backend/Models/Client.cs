namespace fn_backend.Models;

public class Client
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = String.Empty;
    public string RFC { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ServiceMode { get; set; } = string.Empty;
    public double? MonthlyRate { get; set; } = 0.0;
    public bool IsActive { get; set; } = true;
}