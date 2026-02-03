namespace fs_front.DTO;

public class InvoiceItemDetailDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public int? TicketId { get; set; }
    public string? TicketTitle { get; set; }
}