namespace fs_backend.DTO;

public class NotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = ""; // "ticket_assigned", "quote_accepted", "quote_rejected"
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string IconClass { get; set; } = "bi bi-bell";
    public string IconColor { get; set; } = "#6B46C1";
}

public class SendNotificationDto
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Link { get; set; }
    public string? TargetUserId { get; set; } // Si es null, envía a todos los usuarios relevantes
    public string? TargetRole { get; set; } // Admin, Empleado, Administracion, etc.
    public string IconClass { get; set; } = "bi bi-bell";
    public string IconColor { get; set; } = "#6B46C1";
}
