using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class CreateInvoiceFromQuoteDto
{
    [Required(ErrorMessage = "El ID de la cotización es obligatorio")]
    public int QuoteId { get; set; }
    
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}