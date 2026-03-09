namespace fs_front.DTO;

public enum EmployeeActionType
{
    TicketCreado,
    TicketAsignado,
    ActividadTicket,
    ComentarioTicket,
    CotizacionCreada,
    FacturaCreada,
    FacturaMensual,
    PagoRegistrado,
    ClienteAgregado,
    ProyectoCreado
}

public class EmployeeSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
}

public class EmployeeActionDto
{
    public int Id { get; set; }
    public EmployeeActionType ActionType { get; set; }
    public string ActionTypeDisplay { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityUrl { get; set; } = string.Empty;
    public int? RelatedClientId { get; set; }
    public string? RelatedClientName { get; set; }
    public int? RelatedProjectId { get; set; }
    public string? RelatedProjectName { get; set; }
    public int? RelatedTicketId { get; set; }
    public string? RelatedTicketTitle { get; set; }
    public DateTime Date { get; set; }
    public string Details { get; set; } = string.Empty;
    public string IconCss { get; set; } = string.Empty;
    public string ColorCss { get; set; } = string.Empty;
}

public class EmployeeStatsDto
{
    public int TotalTicketsCreados { get; set; }
    public int TotalTicketsAsignados { get; set; }
    public int TotalActividades { get; set; }
    public int TotalComentarios { get; set; }
    public int TotalCotizaciones { get; set; }
    public int TotalFacturas { get; set; }
    public int TotalFacturasMensuales { get; set; }
    public int TotalPagosRegistrados { get; set; }
    public int TotalClientes { get; set; }
    public int TotalProyectos { get; set; }
    public int TotalAcciones { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public class SupervisorFiltersDto
{
    public List<string> Departments { get; set; } = new();
    public List<string> Positions { get; set; } = new();
    public List<ActionTypeOption> ActionTypes { get; set; } = new();
}

public class ActionTypeOption
{
    public string Value { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
}
