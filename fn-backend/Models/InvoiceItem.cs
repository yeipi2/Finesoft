using fn_backend.Models;

namespace fs_backend.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    
    public int? ServiceId { get; set; }
    public Service? Service { get; set; }
    
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }
}