using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fn_backend.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty; // UserId de Identity

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? Link { get; set; }

    public string IconClass { get; set; } = "bi bi-bell";

    public string IconColor { get; set; } = "#6B46C1";

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Nuevos campos para el sistema de notificaciones mejorado
    public string? TargetRole { get; set; } // "Admin" | "Administracion" | "Empleado" | "Cliente"
    public string Severity { get; set; } = "info"; // "info" | "warning" | "error" | "success" | "action"
}

public enum NotificationType
{
    // Tickets
    TicketCreatedByClient,
    TicketCreatedByEmployee,
    TicketAssigned,
    TicketReassigned,
    TicketClosed,
    TicketComment,
    TicketActivityCompleted,

    // Cotizaciones
    QuoteCreated,
    QuoteSent,
    QuoteAccepted,
    QuoteRejected,

    // Facturas
    InvoiceCreated,
    InvoicePaid,
    InvoiceCancelled,
    InvoiceExpiringSoon,

    // Clientes/Proyectos
    ClientCreated,
    ProjectCreated,

    // Empleados
    EmployeeDeactivated,

    // Sistema
    SystemError
}

public static class NotificationDefaults
{
    // Iconos por defecto según tipo
    public static readonly Dictionary<NotificationType, (string IconClass, string IconColor, string Severity)> Defaults = new()
    {
        // Tickets
        { NotificationType.TicketCreatedByClient, ("bi bi-ticket-detailed", "#6B46C1", "info") },
        { NotificationType.TicketCreatedByEmployee, ("bi bi-ticket-detailed", "#6B46C1", "info") },
        { NotificationType.TicketAssigned, ("bi bi-person-check", "#2563EB", "action") },
        { NotificationType.TicketReassigned, ("bi bi-arrow-left-right", "#F59E0B", "warning") },
        { NotificationType.TicketClosed, ("bi bi-check-circle", "#10B981", "success") },
        { NotificationType.TicketComment, ("bi bi-chat-dots", "#8B5CF6", "info") },
        { NotificationType.TicketActivityCompleted, ("bi bi-check2-all", "#10B981", "success") },

        // Cotizaciones
        { NotificationType.QuoteCreated, ("bi bi-file-earmark-text", "#6B46C1", "info") },
        { NotificationType.QuoteSent, ("bi bi-send", "#2563EB", "info") },
        { NotificationType.QuoteAccepted, ("bi bi-check-circle-fill", "#10B981", "success") },
        { NotificationType.QuoteRejected, ("bi bi-x-circle-fill", "#EF4444", "error") },

        // Facturas
        { NotificationType.InvoiceCreated, ("bi bi-receipt", "#6B46C1", "info") },
        { NotificationType.InvoicePaid, ("bi bi-cash-stack", "#10B981", "success") },
        { NotificationType.InvoiceCancelled, ("bi bi-x-octagon", "#EF4444", "error") },
        { NotificationType.InvoiceExpiringSoon, ("bi bi-exclamation-triangle", "#F59E0B", "warning") },

        // Clientes/Proyectos
        { NotificationType.ClientCreated, ("bi bi-person-plus", "#6B46C1", "info") },
        { NotificationType.ProjectCreated, ("bi bi-folder-plus", "#6B46C1", "info") },

        // Empleados
        { NotificationType.EmployeeDeactivated, ("bi bi-person-dash", "#EF4444", "warning") },

        // Sistema
        { NotificationType.SystemError, ("bi bi-exclamation-octagon", "#EF4444", "error") }
    };
}