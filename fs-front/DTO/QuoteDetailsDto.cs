public class QuoteDetailDto
{
    public int Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    // ⭐ NUEVO: Agregar esta propiedad
    public int? InvoiceId { get; set; }

    public List<QuoteItemDto> Items { get; set; } = new();
}