namespace fs_front.DTO;

public class InvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string ClientRFC { get; set; } = string.Empty;

    public int? QuoteId { get; set; }
    public string? QuoteNumber { get; set; }

    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }

    public string InvoiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }

    public DateTime? PaidDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public List<InvoiceItemDetailDto> Items { get; set; } = new();
    public List<InvoicePaymentDto> Payments { get; set; } = new();

    public DateTime? CancelledDate { get; set; }
    public string? CancellationReason { get; set; }

}