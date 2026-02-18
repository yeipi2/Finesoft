using fn_backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fs_backend.Models;

public class Quote
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string QuoteNumber { get; set; } = string.Empty;

    [Required]
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; } // ⭐ AGREGAR ESTE CAMPO

    public DateTime? ValidUntil { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Borrador";

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tax { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    public string? Notes { get; set; }

    public string? PublicToken { get; set; } // ⭐ Token para acceso público

    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}