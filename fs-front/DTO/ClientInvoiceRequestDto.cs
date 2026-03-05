// fs-front/DTO/ClientInvoiceRequestDto.cs
namespace fs_front.DTO;

/// <summary>
/// Representa un cliente seleccionado para generar factura mensual,
/// con el método de pago y forma de pago elegidos manualmente.
/// </summary>
public class ClientInvoiceRequestDto
{
    public int ClientId { get; set; }
    /// <summary>"PPD" | "PUE"</summary>
    public string PaymentMethod { get; set; } = string.Empty;
    /// <summary>Transferencia | Efectivo | Tarjeta | etc. — requerido solo si PaymentMethod = "PUE"</summary>
    public string? PaymentForm { get; set; }
}