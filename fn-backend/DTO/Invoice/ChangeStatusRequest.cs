namespace fs_backend.DTO;

public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty;

    // Solo requerido cuando Status == "Cancelada"
    public string? Reason { get; set; }
}
