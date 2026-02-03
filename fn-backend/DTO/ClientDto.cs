using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class ClientDto
{
    public int? Id { get; set; }
    [Required] public string CompanyName { get; set; } = string.Empty;
    [Required] public string ContactName { get; set; } = string.Empty;
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    [Phone] public string Phone { get; set; } = string.Empty;
    [Required] public string RFC { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    [Required] public string ServiceMode { get; set; } = string.Empty;
    public double? MonthlyRate { get; set; }
    public bool IsActive { get; set; } = true;
}