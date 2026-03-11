// fs-backend/DTO/GenerateMonthlyInvoicesDto.cs
namespace fs_backend.DTO;

/// <summary>
/// Cada item representa un cliente seleccionado con su método de pago elegido.
/// </summary>
public class GenerateMonthlyInvoiceItemDto
{
    public int ClientId { get; set; }
    /// <summary>"PPD" | "PUE"</summary>
    public string PaymentMethod { get; set; } = string.Empty;
    /// <summary>Forma de pago (Transferencia, Efectivo, etc.) — requerido solo cuando PaymentMethod = "PUE"</summary>
    public string? PaymentForm { get; set; }
}

/// <summary>
/// Body del POST api/invoices/generate-monthly
/// </summary>
public class GenerateMonthlyInvoicesDto
{
    public List<GenerateMonthlyInvoiceItemDto> Items { get; set; } = new();
}