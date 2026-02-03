using System.ComponentModel.DataAnnotations;

namespace fs_backend.DTO;

public class QuoteDto
{
    public int? Id { get; set; }
    
    [Required(ErrorMessage = "El cliente es obligatorio")]
    public int ClientId { get; set; }
    
    public DateTime? ValidUntil { get; set; }
    public string Status { get; set; } = "Borrador";
    public string Notes { get; set; } = string.Empty;
    
    public List<QuoteItemDto> Items { get; set; } = new List<QuoteItemDto>();
}