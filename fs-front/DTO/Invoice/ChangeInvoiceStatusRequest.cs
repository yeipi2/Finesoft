namespace fs_front.DTO;

public class ChangeInvoiceStatusRequest
{
    public string Status { get; set; } = "Pendiente";
    public string? Reason { get; set; }
}
