using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class InvoiceDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El cliente es obligatorio")]
    public int ClientId { get; set; }

    public int? QuoteId { get; set; }

    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }

    [Required]
    public string InvoiceType { get; set; } = "Evento";

    public string Status { get; set; } = "Pendiente";
    public string PaymentMethod { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public List<InvoiceItemDto> Items { get; set; } = new();
    [Required]
    public string PaymentType { get; set; } = "PPD"; // "PUE" | "PPD"
}