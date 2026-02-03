using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class InvoiceItemDto
{
    public int? Id { get; set; }
    
    [Required(ErrorMessage = "La descripción es obligatoria")]
    public string Description { get; set; } = string.Empty;
    
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Quantity { get; set; } = 1;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor a 0")]
    public decimal UnitPrice { get; set; }
    
    public int? ServiceId { get; set; }
    public int? TicketId { get; set; }
}