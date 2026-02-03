using fn_backend.Models;

namespace fs_backend.Models;

public class Quote
{
    public int Id { get; set; }
    public string QuoteNumber { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string Status { get; set; }
    
    public string CreatedByUserId { get; set; }
    
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    
    public string Notes { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}