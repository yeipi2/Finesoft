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

    public string? ReceiptFileName { get; set; }
    public string? ReceiptContentType { get; set; }
    public string? ReceiptPath { get; set; }          // ruta relativa tipo "uploads/invoices/12/payments/xxx.pdf"
    public long? ReceiptSize { get; set; }
    public DateTime? ReceiptUploadedAt { get; set; }

}