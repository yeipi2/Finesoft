using fn_backend.Models;

namespace fs_backend.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client Client { get; set; }

    public int? QuoteId { get; set; }
    public Quote? Quote { get; set; }

    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }

    public string InvoiceType { get; set; } = "Evento";
    public string Status { get; set; } = "Pendiente";
    public string PaymentMethod { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public DateTime? PaidDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
}