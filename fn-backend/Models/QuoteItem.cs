namespace fs_backend.Models;

public class QuoteItem
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public Quote? Quote { get; set; }

    // CÓDIGO FUTURO - Relación con Service deshabilitada
    // Descomentar cuando se requiera reactivar la funcionalidad de servicios
    // public int? ServiceId { get; set; }
    // public Service? Service { get; set; }

    // ⭐ NUEVO: Relación con Ticket
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}