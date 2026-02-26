using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class ProfileUpdateDto
{
    // Común
    [EmailAddress]
    public string? Email { get; set; }

    // Empleado
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }

    // Cliente
    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string? RFC { get; set; }
    public string? Address { get; set; }
}