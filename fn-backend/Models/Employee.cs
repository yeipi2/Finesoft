using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fn_backend.Models;

/// <summary>
/// Representa un empleado de la empresa vinculado a un usuario del sistema
/// </summary>
public class Employee
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario asociado (IdentityUser) - REQUERIDO
    /// </summary>
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
    public string Position { get; set; } = string.Empty; // Puesto/Cargo

    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Salary { get; set; } // Opcional y sensible

    public bool IsActive { get; set; } = true;

    // Campos de auditoría
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Información de emergencia
    [MaxLength(200)]
    public string EmergencyContactName { get; set; } = string.Empty;

    [Phone]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    // Notas adicionales
    public string Notes { get; set; } = string.Empty;
}