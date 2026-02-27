// fs-backend/DTO/GenerateMonthlyInvoicesDto.cs
namespace fs_backend.DTO;

/// <summary>
/// Recibe la lista de ClientIds seleccionados por el usuario en el panel de facturas mensuales
/// </summary>
public class GenerateMonthlyInvoicesDto
{
    /// <summary>
    /// IDs de los clientes para los que se desea generar factura mensual.
    /// Si está vacío o es null, se generan todas las pendientes.
    /// </summary>
    public List<int> ClientIds { get; set; } = new();
}