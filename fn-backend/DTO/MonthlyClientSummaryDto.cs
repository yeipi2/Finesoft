// fs-backend/DTO/MonthlyClientSummaryDto.cs
namespace fs_backend.DTO;

public class MonthlyClientSummaryDto
{
    public int ClientId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal MonthlyRate { get; set; }
    public decimal MonthlyHours { get; set; }
    public decimal HoursUsed { get; set; }
    public bool AlreadyHasInvoice { get; set; }

    /// <summary>
    /// Cuando ya existe factura activa (no cancelada) del mes. Si está cancelada, AlreadyHasInvoice = false.
    /// </summary>
    public bool HasCancelledInvoice { get; set; }

    /// <summary>"OK" | "Warning" | "Exceeded" | "Critical"</summary>
    public string HoursStatus { get; set; } = "OK";

    /// <summary>Tickets trabajados durante el mes actual para este cliente</summary>
    public List<MonthlyTicketSummaryDto> Tickets { get; set; } = new();

    public decimal ExcessHours => HoursUsed > MonthlyHours ? HoursUsed - MonthlyHours : 0;
    public double UsagePercent => MonthlyHours > 0 ? (double)(HoursUsed / MonthlyHours) * 100 : 0;
}

public class MonthlyTicketSummaryDto
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal ActualHours { get; set; }
    public DateTime? UpdatedAt { get; set; }
}