using fn_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fn_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SupervisorController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<SupervisorController> _logger;

    public SupervisorController(
        ApplicationDbContext context, 
        UserManager<IdentityUser> userManager,
        ILogger<SupervisorController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    private string? GetCurrentUserId() => 
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// GET: api/supervisor/employees
    /// Lista de empleados con filtros y paginación
    /// </summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees([FromQuery] EmployeeHistoryRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Usuario {UserId} consultando lista de empleados para supervisión", userId);

        var employees = await _context.Employees
            .OrderByDescending(e => e.HireDate)
            .ToListAsync();

        var employeeDtos = new List<EmployeeSummaryDto>();

        foreach (var emp in employees)
        {
            var user = await _userManager.FindByIdAsync(emp.UserId);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            
            var lastActivity = await GetLastActivityDateAsync(emp.UserId);

            employeeDtos.Add(new EmployeeSummaryDto
            {
                UserId = emp.UserId,
                FullName = emp.FullName,
                Email = user?.Email ?? "",
                Position = emp.Position,
                Department = emp.Department,
                RoleName = roles.FirstOrDefault() ?? "",
                IsActive = emp.IsActive,
                HireDate = emp.HireDate,
                LastActivityDate = lastActivity
            });
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            employeeDtos = employeeDtos.Where(e => 
                e.FullName.ToLower().Contains(search) ||
                e.Email.ToLower().Contains(search) ||
                e.Position.ToLower().Contains(search)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.DepartmentFilter) && request.DepartmentFilter != "Todos")
        {
            employeeDtos = employeeDtos.Where(e => e.Department == request.DepartmentFilter).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.PositionFilter) && request.PositionFilter != "Todos")
        {
            employeeDtos = employeeDtos.Where(e => e.Position == request.PositionFilter).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.StatusFilter) && request.StatusFilter != "Todos")
        {
            var isActive = request.StatusFilter == "Activos";
            employeeDtos = employeeDtos.Where(e => e.IsActive == isActive).ToList();
        }

        if (request.DateFrom.HasValue)
        {
            employeeDtos = employeeDtos.Where(e => e.HireDate >= request.DateFrom.Value).ToList();
        }

        if (request.DateTo.HasValue)
        {
            employeeDtos = employeeDtos.Where(e => e.HireDate <= request.DateTo.Value).ToList();
        }

        var totalCount = employeeDtos.Count;
        var pagedEmployees = employeeDtos
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Ok(new PaginatedResult<EmployeeSummaryDto>
        {
            Items = pagedEmployees,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    private async Task<DateTime?> GetLastActivityDateAsync(string userId)
    {
        var lastTicket = await _context.Tickets
            .Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        var lastQuote = await _context.Quotes
            .Where(q => q.CreatedByUserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => q.CreatedAt)
            .FirstOrDefaultAsync();

        var lastInvoice = await _context.Invoices
            .Where(i => i.CreatedByUserId == userId)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => i.InvoiceDate)
            .FirstOrDefaultAsync();

        var lastActivity = await _context.TicketActivities
            .Where(a => a.CreatedByUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        var lastComment = await _context.TicketComments
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        var dates = new List<DateTime>();
        if (lastTicket != default) dates.Add(lastTicket);
        if (lastQuote != default) dates.Add(lastQuote);
        if (lastInvoice != default) dates.Add(lastInvoice);
        if (lastActivity != default) dates.Add(lastActivity);
        if (lastComment != default) dates.Add(lastComment);

        return dates.OrderByDescending(d => d).FirstOrDefault();
    }

    /// <summary>
    /// GET: api/supervisor/employees/{userId}/history
    /// Historial completo de acciones de un empleado
    /// </summary>
    [HttpGet("employees/{userId}/history")]
    public async Task<IActionResult> GetEmployeeHistory(string userId, [FromQuery] EmployeeActionsRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Usuario {UserId} consultando historial del empleado {TargetUserId}", currentUserId, userId);

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null)
            return NotFound(new { message = "Empleado no encontrado" });

        var actions = new List<EmployeeActionDto>();
        var actionId = 1;

        var ticketsCreados = await _context.Tickets
            .Include(t => t.Project)
            .ThenInclude(p => p != null ? p.Client : null)
            .Where(t => t.CreatedByUserId == userId)
            .ToListAsync();

        foreach (var t in ticketsCreados)
        {
            var action = new EmployeeActionDto
            {
                Id = actionId++,
                ActionType = EmployeeActionType.TicketCreado,
                ActionTypeDisplay = "Ticket Creado",
                EntityId = t.Id,
                EntityName = $"#{t.Id} - {t.Title}",
                EntityUrl = $"/tickets/{t.Id}",
                RelatedClientId = t.Project?.ClientId,
                RelatedClientName = t.Project?.Client?.CompanyName,
                RelatedProjectId = t.ProjectId,
                RelatedProjectName = t.Project?.Name,
                Date = t.CreatedAt,
                Details = $"Status: {t.Status} | Prioridad: {t.Priority}",
                IconCss = "bi-ticket-perforated",
                ColorCss = "#1d4ed8"
            };

            if (request.ActionTypeFilter == null || request.ActionTypeFilter == EmployeeActionType.TicketCreado)
            {
                if (IsDateInRange(action.Date, request.DateFrom, request.DateTo))
                    actions.Add(action);
            }
        }

        var ticketsAsignados = await _context.Tickets
            .Include(t => t.Project)
            .ThenInclude(p => p != null ? p.Client : null)
            .Where(t => t.AssignedToUserId == userId && t.CreatedByUserId != userId)
            .ToListAsync();

        foreach (var t in ticketsAsignados)
        {
            var action = new EmployeeActionDto
            {
                Id = actionId++,
                ActionType = EmployeeActionType.TicketAsignado,
                ActionTypeDisplay = "Ticket Asignado",
                EntityId = t.Id,
                EntityName = $"#{t.Id} - {t.Title}",
                EntityUrl = $"/tickets/{t.Id}",
                RelatedClientId = t.Project?.ClientId,
                RelatedClientName = t.Project?.Client?.CompanyName,
                RelatedProjectId = t.ProjectId,
                RelatedProjectName = t.Project?.Name,
                Date = t.CreatedAt,
                Details = $"Status: {t.Status} | Prioridad: {t.Priority}",
                IconCss = "bi-person-check",
                ColorCss = "#7c3aed"
            };

            if (request.ActionTypeFilter == null || request.ActionTypeFilter == EmployeeActionType.TicketAsignado)
            {
                if (IsDateInRange(action.Date, request.DateFrom, request.DateTo))
                    actions.Add(action);
            }
        }

        var actividades = await _context.TicketActivities
            .Include(a => a.Ticket)
            .ThenInclude(t => t != null ? t.Project : null)
            .ThenInclude(p => p != null ? p.Client : null)
            .Where(a => a.CreatedByUserId == userId)
            .ToListAsync();

        foreach (var a in actividades)
        {
            var action = new EmployeeActionDto
            {
                Id = actionId++,
                ActionType = EmployeeActionType.ActividadTicket,
                ActionTypeDisplay = "Actividad de Ticket",
                EntityId = a.Id,
                EntityName = $"#{a.TicketId} - {TruncateString(a.Description, 50)}",
                EntityUrl = $"/tickets/{a.TicketId}",
                RelatedTicketId = a.TicketId,
                RelatedTicketTitle = a.Ticket?.Title,
                RelatedClientId = a.Ticket?.Project?.ClientId,
                RelatedClientName = a.Ticket?.Project?.Client?.CompanyName,
                RelatedProjectId = a.Ticket?.ProjectId,
                RelatedProjectName = a.Ticket?.Project?.Name,
                Date = a.CreatedAt,
                Details = $"Horas: {a.HoursSpent} | Completada: {(a.IsCompleted ? "Sí" : "No")}",
                IconCss = "bi-check2-square",
                ColorCss = "#059669"
            };

            if (request.ActionTypeFilter == null || request.ActionTypeFilter == EmployeeActionType.ActividadTicket)
            {
                if (IsDateInRange(action.Date, request.DateFrom, request.DateTo))
                    actions.Add(action);
            }
        }

        var comentarios = await _context.TicketComments
            .Include(c => c.Ticket)
            .ThenInclude(t => t != null ? t.Project : null)
            .ThenInclude(p => p != null ? p.Client : null)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        foreach (var c in comentarios)
        {
            var action = new EmployeeActionDto
            {
                Id = actionId++,
                ActionType = EmployeeActionType.ComentarioTicket,
                ActionTypeDisplay = "Comentario en Ticket",
                EntityId = c.Id,
                EntityName = $"#{c.TicketId} - {TruncateString(c.Comment, 50)}",
                EntityUrl = $"/tickets/{c.TicketId}",
                RelatedTicketId = c.TicketId,
                RelatedTicketTitle = c.Ticket?.Title,
                RelatedClientId = c.Ticket?.Project?.ClientId,
                RelatedClientName = c.Ticket?.Project?.Client?.CompanyName,
                RelatedProjectId = c.Ticket?.ProjectId,
                RelatedProjectName = c.Ticket?.Project?.Name,
                Date = c.CreatedAt,
                Details = TruncateString(c.Comment, 100),
                IconCss = "bi-chat-dots",
                ColorCss = "#0891b2"
            };

            if (request.ActionTypeFilter == null || request.ActionTypeFilter == EmployeeActionType.ComentarioTicket)
            {
                if (IsDateInRange(action.Date, request.DateFrom, request.DateTo))
                    actions.Add(action);
            }
        }

        var cotizaciones = await _context.Quotes
            .Include(q => q.Client)
            .Where(q => q.CreatedByUserId == userId)
            .ToListAsync();

        foreach (var q in cotizaciones)
        {
            var action = new EmployeeActionDto
            {
                Id = actionId++,
                ActionType = EmployeeActionType.CotizacionCreada,
                ActionTypeDisplay = "Cotización Creada",
                EntityId = q.Id,
                EntityName = $"#{q.QuoteNumber} - {q.Client?.CompanyName}",
                EntityUrl = $"/cotizaciones",
                RelatedClientId = q.ClientId,
                RelatedClientName = q.Client?.CompanyName,
                Date = q.CreatedAt,
                Details = $"Total: ${q.Total:N2} | Status: {q.Status}",
                IconCss = "bi-file-earmark-text",
                ColorCss = "#dc2626"
            };

            if (request.ActionTypeFilter == null || request.ActionTypeFilter == EmployeeActionType.CotizacionCreada)
            {
                if (IsDateInRange(action.Date, request.DateFrom, request.DateTo))
                    actions.Add(action);
            }
        }

        var facturas = await _context.Invoices
            .Include(i => i.Client)
            .Where(i => i.CreatedByUserId == userId)
            .ToListAsync();

        foreach (var f in facturas)
        {
            var isMonthly = f.InvoiceType == "Mensual";
            var action = new EmployeeActionDto
            {
                Id = actionId++,
                ActionType = isMonthly ? EmployeeActionType.FacturaMensual : EmployeeActionType.FacturaCreada,
                ActionTypeDisplay = isMonthly ? "Factura Mensual" : "Factura Creada",
                EntityId = f.Id,
                EntityName = $"#{f.InvoiceNumber} - {f.Client?.CompanyName}",
                EntityUrl = $"/facturas/{f.Id}",
                RelatedClientId = f.ClientId,
                RelatedClientName = f.Client?.CompanyName,
                Date = f.InvoiceDate,
                Details = $"Total: ${f.Total:N2} | Status: {f.Status} | Tipo: {f.InvoiceType}",
                IconCss = isMonthly ? "bi-calendar-check" : "bi-receipt-cutoff",
                ColorCss = isMonthly ? "#7c3aed" : "#ea580c"
            };

            var filterType = isMonthly ? EmployeeActionType.FacturaMensual : EmployeeActionType.FacturaCreada;
            if (request.ActionTypeFilter == null || request.ActionTypeFilter == filterType)
            {
                if (IsDateInRange(action.Date, request.DateFrom, request.DateTo))
                    actions.Add(action);
            }
        }

        var pagos = await _context.InvoicePayments
            .Include(p => p.Invoice)
            .ThenInclude(i => i != null ? i.Client : null)
            .Where(p => p.RecordedByUserId == userId)
            .ToListAsync();

        foreach (var p in pagos)
        {
            var action = new EmployeeActionDto
            {
                Id = actionId++,
                ActionType = EmployeeActionType.PagoRegistrado,
                ActionTypeDisplay = "Pago Registrado",
                EntityId = p.Id,
                EntityName = $"#{p.Invoice?.InvoiceNumber} - ${p.Amount:N2}",
                EntityUrl = $"/facturas/{p.InvoiceId}",
                RelatedClientId = p.Invoice?.ClientId,
                RelatedClientName = p.Invoice?.Client?.CompanyName,
                Date = p.PaymentDate,
                Details = $"Método: {p.PaymentMethod} | Referencia: {p.Reference}",
                IconCss = "bi-credit-card",
                ColorCss = "#059669"
            };

            if (request.ActionTypeFilter == null || request.ActionTypeFilter == EmployeeActionType.PagoRegistrado)
            {
                if (IsDateInRange(action.Date, request.DateFrom, request.DateTo))
                    actions.Add(action);
            }
        }

        var orderedActions = actions.OrderByDescending(a => a.Date).ToList();
        var totalCount = orderedActions.Count;
        var pagedActions = orderedActions
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        for (int i = 0; i < pagedActions.Count; i++)
        {
            pagedActions[i].Id = i + 1;
        }

        return Ok(new PaginatedResult<EmployeeActionDto>
        {
            Items = pagedActions,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    /// <summary>
    /// GET: api/supervisor/employees/{userId}/stats
    /// Estadísticas de un empleado
    /// </summary>
    [HttpGet("employees/{userId}/stats")]
    public async Task<IActionResult> GetEmployeeStats(string userId)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Usuario {UserId} consultando estadísticas del empleado {TargetUserId}", currentUserId, userId);

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null)
            return NotFound(new { message = "Empleado no encontrado" });

        var stats = new EmployeeStatsDto
        {
            TotalTicketsCreados = await _context.Tickets.CountAsync(t => t.CreatedByUserId == userId),
            TotalTicketsAsignados = await _context.Tickets.CountAsync(t => t.AssignedToUserId == userId && t.CreatedByUserId != userId),
            TotalActividades = await _context.TicketActivities.CountAsync(a => a.CreatedByUserId == userId),
            TotalComentarios = await _context.TicketComments.CountAsync(c => c.UserId == userId),
            TotalCotizaciones = await _context.Quotes.CountAsync(q => q.CreatedByUserId == userId),
            TotalFacturas = await _context.Invoices.CountAsync(i => i.CreatedByUserId == userId && i.InvoiceType != "Mensual"),
            TotalFacturasMensuales = await _context.Invoices.CountAsync(i => i.CreatedByUserId == userId && i.InvoiceType == "Mensual"),
            TotalPagosRegistrados = await _context.InvoicePayments.CountAsync(p => p.RecordedByUserId == userId),
            TotalClientes = 0,
            TotalProyectos = 0
        };

        return Ok(stats);
    }

    /// <summary>
    /// GET: api/supervisor/employees/{userId}/details
    /// Detalles completos de un empleado
    /// </summary>
    [HttpGet("employees/{userId}/details")]
    public async Task<IActionResult> GetEmployeeDetails(string userId)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Usuario {UserId} consultando detalles del empleado {TargetUserId}", currentUserId, userId);

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null)
            return NotFound(new { message = "Empleado no encontrado" });

        var user = await _userManager.FindByIdAsync(employee.UserId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

        var summary = new EmployeeSummaryDto
        {
            UserId = employee.UserId,
            FullName = employee.FullName,
            Email = user?.Email ?? "",
            Position = employee.Position,
            Department = employee.Department,
            RoleName = roles.FirstOrDefault() ?? "",
            IsActive = employee.IsActive,
            HireDate = employee.HireDate,
            LastActivityDate = await GetLastActivityDateAsync(userId)
        };

        return Ok(summary);
    }

    /// <summary>
    /// GET: api/supervisor/filters
    /// Obtener filtros disponibles (departamentos, puestos)
    /// </summary>
    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters()
    {
        var departments = await _context.Employees
            .Where(e => !string.IsNullOrEmpty(e.Department))
            .Select(e => e.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var positions = await _context.Employees
            .Where(e => !string.IsNullOrEmpty(e.Position))
            .Select(e => e.Position)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var actionTypes = Enum.GetValues<EmployeeActionType>()
            .Select(t => new { Value = t.ToString(), Display = GetActionTypeDisplay(t) })
            .ToList();

        return Ok(new
        {
            Departments = departments,
            Positions = positions,
            ActionTypes = actionTypes
        });
    }

    private bool IsDateInRange(DateTime date, DateTime? from, DateTime? to)
    {
        if (from.HasValue && date < from.Value.Date) return false;
        if (to.HasValue && date > to.Value.Date) return false;
        return true;
    }

    private string TruncateString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        return str.Length <= maxLength ? str : str.Substring(0, maxLength) + "...";
    }

    private string GetActionTypeDisplay(EmployeeActionType type)
    {
        return type switch
        {
            EmployeeActionType.TicketCreado => "Tickets Creados",
            EmployeeActionType.TicketAsignado => "Tickets Asignados",
            EmployeeActionType.ActividadTicket => "Actividades de Tickets",
            EmployeeActionType.ComentarioTicket => "Comentarios en Tickets",
            EmployeeActionType.CotizacionCreada => "Cotizaciones",
            EmployeeActionType.FacturaCreada => "Facturas",
            EmployeeActionType.FacturaMensual => "Facturas Mensuales",
            EmployeeActionType.PagoRegistrado => "Pagos Registrados",
            EmployeeActionType.ClienteAgregado => "Clientes Agregados",
            EmployeeActionType.ProyectoCreado => "Proyectos Creados",
            _ => type.ToString()
        };
    }
}
