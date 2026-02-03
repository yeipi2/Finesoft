using fn_backend.Models;

namespace fs_backend.Models;

public class QuoteItem
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public Quote Quote { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    
    public int? ServiceId { get; set; }
    public Service? Service { get; set; }
}