namespace fs_backend.Models;

public class InvoicePayment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; }
    
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    
    public string RecordedByUserId { get; set; } = string.Empty;
}