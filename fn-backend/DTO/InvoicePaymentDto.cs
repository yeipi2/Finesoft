using System.ComponentModel.DataAnnotations;

namespace fs_backend.DTO;

public class InvoicePaymentDto
{
    public int? Id { get; set; }
    
    [Required(ErrorMessage = "El monto es obligatorio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal Amount { get; set; }
    
    [Required(ErrorMessage = "La fecha de pago es obligatoria")]
    public DateTime PaymentDate { get; set; }
    
    [Required(ErrorMessage = "El método de pago es obligatorio")]
    public string PaymentMethod { get; set; } = string.Empty;
    
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    
    public string? RecordedByUserId { get; set; }
    public string? RecordedByUserName { get; set; }
}