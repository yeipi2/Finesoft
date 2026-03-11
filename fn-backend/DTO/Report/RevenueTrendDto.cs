namespace fs_backend.DTO;

public class RevenueTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }         // Pagado ese mes (para gráfica de tendencia)
    public decimal TotalInvoiced { get; set; }   // Total facturado ese mes
    public decimal TotalPaid { get; set; }       // Total pagado ese mes
    public decimal TotalPending { get; set; }    // Total pendiente ese mes
    public int InvoicesCount { get; set; }
}