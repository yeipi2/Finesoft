namespace fn_backend.DTO;

public class ProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Empleado
    public int? EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }

    // Cliente
    public int? ClientId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string RFC { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ServiceMode { get; set; } = string.Empty;
    public double? MonthlyRate { get; set; }
}