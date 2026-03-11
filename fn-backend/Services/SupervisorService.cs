using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fn_backend.Services;

public interface ISupervisorService
{
    Task<PaginatedResult<EmployeeSummaryDto>> GetEmployeesAsync(
        int page, int pageSize, string? search, string? departmentFilter,
        string? positionFilter, string? statusFilter, DateTime? dateFrom, DateTime? dateTo);

    Task<EmployeeSummaryDto?> GetEmployeeDetailsAsync(string userId);

    Task<List<string>> GetDepartmentsAsync();
    Task<List<string>> GetPositionsAsync();

    Task<PaginatedResult<EmployeeActionDto>> GetEmployeeHistoryAsync(
        string userId, int page, int pageSize, EmployeeActionType? actionTypeFilter,
        DateTime? dateFrom, DateTime? dateTo);

    Task<EmployeeStatsDto> GetEmployeeStatsAsync(string userId);
}

public class SupervisorService : ISupervisorService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SupervisorService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PaginatedResult<EmployeeSummaryDto>> GetEmployeesAsync(
        int page, int pageSize, string? search, string? departmentFilter,
        string? positionFilter, string? statusFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        page = page > 0 ? page : 1;
        pageSize = pageSize > 0 ? pageSize : 15;

        var query = BuildEmployeeQuery(search, departmentFilter, positionFilter, statusFilter, dateFrom, dateTo);

        var totalCount = await query.CountAsync();

        var results = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = results.Select(r => r.Employee.UserId).ToList();
        
        var userRoles = await GetUserRolesAsync(userIds);
        var lastActivities = await GetLastActivitiesBatchAsync(userIds);

        var employeeDtos = results.Select(r =>
        {
            var role = userRoles.FirstOrDefault(ur => ur.UserId == r.Employee.UserId);
            return new EmployeeSummaryDto
            {
                UserId = r.Employee.UserId,
                FullName = r.Employee.FullName,
                Email = r.Email,
                Position = r.Employee.Position,
                Department = r.Employee.Department,
                RoleName = role.UserId != null ? role.Name : "",
                IsActive = r.Employee.IsActive,
                HireDate = r.Employee.HireDate,
                LastActivityDate = lastActivities.GetValueOrDefault(r.Employee.UserId)
            };
        }).ToList();

        return new PaginatedResult<EmployeeSummaryDto>
        {
            Items = employeeDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private IQueryable<EmployeeQueryResult> BuildEmployeeQuery(
        string? search, string? departmentFilter, string? positionFilter,
        string? statusFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        var query = from emp in _context.Employees
                    join user in _context.Users on emp.UserId equals user.Id into userJoin
                    from user in userJoin.DefaultIfEmpty()
                    orderby emp.HireDate descending
                    select new EmployeeQueryResult
                    {
                        Employee = emp,
                        Email = user != null ? user.Email : ""
                    };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(x => 
                x.Employee.FullName.ToLower().Contains(searchLower) ||
                x.Email.ToLower().Contains(searchLower) ||
                x.Employee.Position.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(departmentFilter) && departmentFilter != "Todos")
        {
            query = query.Where(x => x.Employee.Department == departmentFilter);
        }

        if (!string.IsNullOrWhiteSpace(positionFilter) && positionFilter != "Todos")
        {
            query = query.Where(x => x.Employee.Position == positionFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "Todos")
        {
            var isActive = statusFilter == "Activos";
            query = query.Where(x => x.Employee.IsActive == isActive);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.Employee.HireDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.Employee.HireDate <= dateTo.Value);
        }

        return query;
    }

    private class EmployeeQueryResult
    {
        public Employee Employee { get; set; } = null!;
        public string Email { get; set; } = "";
    }

    private async Task<List<(string UserId, string Name)>> GetUserRolesAsync(List<string> userIds)
    {
        var roles = await (from ur in _context.UserRoles
                     join r in _context.Roles on ur.RoleId equals r.Id
                     where userIds.Contains(ur.UserId)
                     select new { ur.UserId, r.Name })
                    .ToListAsync();
        
        return roles.Select(x => (x.UserId, x.Name)).ToList();
    }

    private async Task<Dictionary<string, DateTime?>> GetLastActivitiesBatchAsync(List<string> userIds)
    {
        if (!userIds.Any()) return new Dictionary<string, DateTime?>();

        var allDates = new List<(string UserId, DateTime Date)>();

        var ticketCreated = await _context.Tickets
            .Where(t => userIds.Contains(t.CreatedByUserId))
            .Select(t => new { t.CreatedByUserId, t.CreatedAt })
            .ToListAsync();
        allDates.AddRange(ticketCreated.Select(t => (t.CreatedByUserId, t.CreatedAt)));

        var ticketAssigned = await _context.Tickets
            .Where(t => t.AssignedToUserId != null && userIds.Contains(t.AssignedToUserId))
            .Select(t => new { UserId = t.AssignedToUserId!, t.CreatedAt })
            .ToListAsync();
        allDates.AddRange(ticketAssigned.Select(t => (t.UserId, t.CreatedAt)));

        var quotes = await _context.Quotes
            .Where(q => userIds.Contains(q.CreatedByUserId))
            .Select(q => new { q.CreatedByUserId, q.CreatedAt })
            .ToListAsync();
        allDates.AddRange(quotes.Select(q => (q.CreatedByUserId, q.CreatedAt)));

        var invoices = await _context.Invoices
            .Where(i => userIds.Contains(i.CreatedByUserId))
            .Select(i => new { i.CreatedByUserId, i.InvoiceDate })
            .ToListAsync();
        allDates.AddRange(invoices.Select(i => (i.CreatedByUserId, i.InvoiceDate)));

        var activities = await _context.TicketActivities
            .Where(a => userIds.Contains(a.CreatedByUserId))
            .Select(a => new { a.CreatedByUserId, a.CreatedAt })
            .ToListAsync();
        allDates.AddRange(activities.Select(a => (a.CreatedByUserId, a.CreatedAt)));

        var comments = await _context.TicketComments
            .Where(c => userIds.Contains(c.UserId))
            .Select(c => new { c.UserId, c.CreatedAt })
            .ToListAsync();
        allDates.AddRange(comments.Select(c => (c.UserId, c.CreatedAt)));

        var result = new Dictionary<string, DateTime?>();
        foreach (var uid in userIds)
        {
            var userDates = allDates.Where(x => x.UserId == uid).ToList();
            if (userDates.Any())
            {
                result[uid] = userDates.MaxBy(x => x.Date).Date;
            }
            else
            {
                result[uid] = null;
            }
        }

        return result;
    }

    public async Task<EmployeeSummaryDto?> GetEmployeeDetailsAsync(string userId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return null;

        var user = await _userManager.FindByIdAsync(userId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

        var profile = await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var lastActivities = await GetLastActivitiesBatchAsync(new List<string> { userId });

        return new EmployeeSummaryDto
        {
            UserId = employee.UserId,
            FullName = employee.FullName,
            Email = user?.Email ?? "",
            Position = employee.Position,
            Department = employee.Department,
            RoleName = roles.FirstOrDefault() ?? "",
            IsActive = employee.IsActive,
            HireDate = employee.HireDate,
            LastActivityDate = lastActivities.GetValueOrDefault(userId),
            AvatarDataUrl = profile?.AvatarDataUrl
        };
    }

    public async Task<List<string>> GetDepartmentsAsync()
    {
        return await _context.Employees
            .Where(e => !string.IsNullOrEmpty(e.Department))
            .Select(e => e.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<List<string>> GetPositionsAsync()
    {
        return await _context.Employees
            .Where(e => !string.IsNullOrEmpty(e.Position))
            .Select(e => e.Position)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task<PaginatedResult<EmployeeActionDto>> GetEmployeeHistoryAsync(
        string userId, int page, int pageSize, EmployeeActionType? actionTypeFilter,
        DateTime? dateFrom, DateTime? dateTo)
    {
        var allActions = new List<EmployeeActionDto>();

        var tickets = await GetTicketActionsAsync(userId, actionTypeFilter, dateFrom, dateTo);
        allActions.AddRange(tickets);

        var quotes = await GetQuoteActionsAsync(userId, actionTypeFilter, dateFrom, dateTo);
        allActions.AddRange(quotes);

        var invoices = await GetInvoiceActionsAsync(userId, actionTypeFilter, dateFrom, dateTo);
        allActions.AddRange(invoices);

        var activities = await GetActivityActionsAsync(userId, actionTypeFilter, dateFrom, dateTo);
        allActions.AddRange(activities);

        var comments = await GetCommentActionsAsync(userId, actionTypeFilter, dateFrom, dateTo);
        allActions.AddRange(comments);

        var totalCount = allActions.Count;
        var pagedActions = allActions
            .OrderByDescending(a => a.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<EmployeeActionDto>
        {
            Items = pagedActions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task<List<EmployeeActionDto>> GetTicketActionsAsync(
        string userId, EmployeeActionType? filter, DateTime? from, DateTime? to)
    {
        var query = _context.Tickets
            .Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        var tickets = await query.ToListAsync();

        return tickets.Select(t =>
        {
            var isCreator = t.CreatedByUserId == userId;
            var actionType = isCreator ? EmployeeActionType.TicketCreado : EmployeeActionType.TicketAsignado;

            if (filter.HasValue && filter.Value != actionType)
                return null!;

            return new EmployeeActionDto
            {
                Id = t.Id,
                ActionType = actionType,
                ActionTypeDisplay = isCreator ? "Ticket Creado" : "Ticket Asignado",
                EntityId = t.Id,
                EntityName = t.Title,
                EntityUrl = $"/tickets/{t.Id}",
                Date = t.CreatedAt,
                IconCss = isCreator ? "bi-plus-circle" : "bi-person-check",
                ColorCss = isCreator ? "#1d4ed8" : "#7c3aed"
            };
        }).Where(a => a != null).ToList();
    }

    private async Task<List<EmployeeActionDto>> GetQuoteActionsAsync(
        string userId, EmployeeActionType? filter, DateTime? from, DateTime? to)
    {
        var query = _context.Quotes
            .Include(q => q.Client)
            .Where(q => q.CreatedByUserId == userId)
            .AsQueryable();

        if (from.HasValue) query = query.Where(q => q.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(q => q.CreatedAt <= to.Value);

        var quotes = await query.ToListAsync();

        return quotes.Select(q =>
        {
            if (filter.HasValue && filter.Value != EmployeeActionType.CotizacionCreada)
                return null!;

            return new EmployeeActionDto
            {
                Id = q.Id,
                ActionType = EmployeeActionType.CotizacionCreada,
                ActionTypeDisplay = "Cotización Creada",
                EntityId = q.Id,
                EntityName = q.Client?.CompanyName ?? "Cliente",
                EntityUrl = $"/cotizaciones/{q.Id}",
                Date = q.CreatedAt,
                IconCss = "bi-file-earmark-text",
                ColorCss = "#0d9488"
            };
        }).Where(a => a != null).ToList();
    }

    private async Task<List<EmployeeActionDto>> GetInvoiceActionsAsync(
        string userId, EmployeeActionType? filter, DateTime? from, DateTime? to)
    {
        var query = _context.Invoices
            .Where(i => i.CreatedByUserId == userId)
            .AsQueryable();

        if (from.HasValue) query = query.Where(i => i.InvoiceDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.InvoiceDate <= to.Value);

        var invoices = await query.ToListAsync();

        return invoices.Select(i =>
        {
            if (filter.HasValue && filter.Value != EmployeeActionType.FacturaCreada)
                return null!;

            return new EmployeeActionDto
            {
                Id = i.Id,
                ActionType = EmployeeActionType.FacturaCreada,
                ActionTypeDisplay = "Factura Creada",
                EntityId = i.Id,
                EntityName = $"Factura #{i.InvoiceNumber}",
                EntityUrl = $"/facturas/{i.Id}",
                Date = i.InvoiceDate,
                IconCss = "bi-receipt",
                ColorCss = "#059669"
            };
        }).Where(a => a != null).ToList();
    }

    private async Task<List<EmployeeActionDto>> GetActivityActionsAsync(
        string userId, EmployeeActionType? filter, DateTime? from, DateTime? to)
    {
        var query = _context.TicketActivities
            .Where(a => a.CreatedByUserId == userId)
            .AsQueryable();

        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);

        var activities = await query.ToListAsync();

        return activities.Select(a =>
        {
            if (filter.HasValue && filter.Value != EmployeeActionType.ActividadTicket)
                return null!;

            return new EmployeeActionDto
            {
                Id = a.Id,
                ActionType = EmployeeActionType.ActividadTicket,
                ActionTypeDisplay = "Actividad de Ticket",
                EntityId = a.TicketId,
                EntityName = a.Description,
                Date = a.CreatedAt,
                IconCss = "bi-activity",
                ColorCss = "#d97706"
            };
        }).Where(a => a != null).ToList();
    }

    private async Task<List<EmployeeActionDto>> GetCommentActionsAsync(
        string userId, EmployeeActionType? filter, DateTime? from, DateTime? to)
    {
        var query = _context.TicketComments
            .Where(c => c.UserId == userId)
            .AsQueryable();

        if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);

        var comments = await query.ToListAsync();

        return comments.Select(c =>
        {
            if (filter.HasValue && filter.Value != EmployeeActionType.ComentarioTicket)
                return null!;

            return new EmployeeActionDto
            {
                Id = c.Id,
                ActionType = EmployeeActionType.ComentarioTicket,
                ActionTypeDisplay = "Comentario en Ticket",
                EntityId = c.TicketId,
                EntityName = c.Comment.Length > 50 ? c.Comment.Substring(0, 50) + "..." : c.Comment,
                Date = c.CreatedAt,
                IconCss = "bi-chat-dots",
                ColorCss = "#6366f1"
            };
        }).Where(a => a != null).ToList();
    }

    public async Task<EmployeeStatsDto> GetEmployeeStatsAsync(string userId)
    {
        var ticketsCreados = await _context.Tickets.CountAsync(t => t.CreatedByUserId == userId);
        var ticketsAsignados = await _context.Tickets.CountAsync(t => t.AssignedToUserId == userId);
        var actividades = await _context.TicketActivities.CountAsync(a => a.CreatedByUserId == userId);
        var comentarios = await _context.TicketComments.CountAsync(c => c.UserId == userId);
        var cotizaciones = await _context.Quotes.CountAsync(q => q.CreatedByUserId == userId);
        var facturas = await _context.Invoices.CountAsync(i => i.CreatedByUserId == userId);

        return new EmployeeStatsDto
        {
            TotalTicketsCreados = ticketsCreados,
            TotalTicketsAsignados = ticketsAsignados,
            TotalActividades = actividades,
            TotalComentarios = comentarios,
            TotalCotizaciones = cotizaciones,
            TotalFacturas = facturas
        };
    }
}
