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

    // ⭐ PROPIEDADES COMPLETAS DEL TICKET (igual que QuoteItemDetailDto)
    public int? TicketId { get; set; }
    public string? TicketTitle { get; set; }
    public string? TicketDescription { get; set; }
    public string? TicketClientName { get; set; }
    public string? TicketProjectName { get; set; }
    public decimal? TicketActualHours { get; set; }
    public decimal? TicketHourlyRate { get; set; }
}