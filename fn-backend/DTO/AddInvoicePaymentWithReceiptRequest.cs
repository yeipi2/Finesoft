using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace fs_backend.DTO;

public class AddInvoicePaymentWithReceiptRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    [Required]
    public IFormFile Receipt { get; set; } = default!;
}
