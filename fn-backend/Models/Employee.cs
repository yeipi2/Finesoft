using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fn_backend.Models;


public class Employee
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(13)]
    public string RFC { get; set; } = string.Empty;

    [MaxLength(18)]
    public string CURP { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Position { get; set; } = string.Empty; 

    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Salary { get; set; } 

    public bool IsActive { get; set; } = true;

  
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

   
    [MaxLength(200)]
    public string EmergencyContactName { get; set; } = string.Empty;

    [Phone]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}