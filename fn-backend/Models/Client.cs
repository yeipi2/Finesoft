using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fn_backend.Models;

public class Client
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario asociado (IdentityUser) - OPCIONAL al inicio, REQUERIDO después
    /// </summary>
    /// 
    [MaxLength(450)]
    public string? UserId { get; set; } // ⭐ Relación con IdentityUser

    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string RFC { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;


    [Required]
    public string ServiceMode { get; set; } = string.Empty;

    // ⭐ NUEVA PROPIEDAD: Frecuencia de facturación
    /// <summary>
    /// Frecuencia de facturación: "Event" (por evento) o "Monthly" (mensual)
    /// </summary>
    public string BillingFrequency { get; set; } = "Event";

    public decimal MonthlyHours { get; set; } = 0;

    public double? MonthlyRate { get; set; } = 0.0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Models/Client.cs
    [InverseProperty("Client")]  // ⭐ AGREGAR ESTO
    public ICollection<Project>? Projects { get; set; }
}