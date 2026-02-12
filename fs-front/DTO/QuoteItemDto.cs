public class QuoteItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;

    // Relaciones opcionales
    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }

    // ⭐ NUEVO: Propiedades de ticket
    public int? TicketId { get; set; }
    public string? TicketTitle { get; set; }
    public string? TicketDescription { get; set; }
    public string? TicketClientName { get; set; }
    public string? TicketProjectName { get; set; }
    public decimal? TicketActualHours { get; set; }
    public decimal? TicketHourlyRate { get; set; }
}