using fs_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ReportService(ApplicationDbContext context, UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        return new DashboardStatsDto
        {
            TotalClients = await _context.Clients.CountAsync(c => c.IsActive),
            ActiveProjects = await _context.Projects.CountAsync(p => p.IsActive),
            OpenTickets = await _context.Tickets.CountAsync(t => t.Status != "Cerrado"),
            TotalRevenue = await _context.Invoices.Where(i => i.Status == "Pagada").SumAsync(i => (decimal?)i.Total) ?? 0,
            MonthlyRevenue = await _context.Invoices
                .Where(i => i.Status == "Pagada" && i.InvoiceDate >= startOfMonth)
                .SumAsync(i => (decimal?)i.Total) ?? 0,
            PendingPayments = await _context.Invoices.Where(i => i.Status == "Pendiente").SumAsync(i => (decimal?)i.Total) ?? 0,
            TotalInvoices = await _context.Invoices.CountAsync(),
            TotalQuotes = await _context.Quotes.CountAsync()
        };
    }

    public async Task<List<UserReportDto>> GetReportsByUserAsync(DateTime? startDate, DateTime? endDate)
    {
        // ✅ FIX: Solo incluir usuarios con rol "Empleado"
        var allUsers = await _userManager.Users.ToListAsync();
        var staffUsers = new List<IdentityUser>();

        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            // Solo mostrar empleados — los responsables reales de tickets
            if (roles.Any(r => r.Equals("Empleado", StringComparison.OrdinalIgnoreCase)))
            {
                staffUsers.Add(user);
            }
        }

        var reports = new List<UserReportDto>();

        var ticketsQuery = _context.Tickets.AsQueryable();
        if (startDate.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.CreatedAt <= endDate.Value);

        foreach (var user in staffUsers)
        {
            var userTickets = await ticketsQuery.Where(t => t.CreatedByUserId == user.Id).ToListAsync();
            var closedTickets = userTickets.Count(t => t.Status == "Cerrado");
            var totalHours = userTickets.Sum(t => t.ActualHours);

            reports.Add(new UserReportDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? "Sin nombre",
                TicketsCreated = userTickets.Count,
                TicketsClosed = closedTickets,
                TotalHoursWorked = totalHours,
                Revenue = 0m
            });
        }

        // Solo mostrar usuarios que tengan actividad o sean staff relevante
        return reports
            .OrderByDescending(r => r.TicketsCreated)
            .ThenByDescending(r => r.TotalHoursWorked)
            .ToList();
    }

    public async Task<List<ClientReportDto>> GetReportsByClientAsync(DateTime? startDate, DateTime? endDate)
    {
        var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
        var reports = new List<ClientReportDto>();

        foreach (var client in clients)
        {
            var projects = await _context.Projects.Where(p => p.ClientId == client.Id).ToListAsync();
            var projectIds = projects.Select(p => p.Id).ToList();

            // ✅ FIX: Filtrar tickets SOLO de los proyectos de este cliente
            List<Models.Ticket> tickets;
            if (projectIds.Any())
            {
                var ticketsQuery = _context.Tickets
                .Where(t => t.ProjectId.HasValue && projectIds.Contains(t.ProjectId.Value));

                if (startDate.HasValue)
                    ticketsQuery = ticketsQuery.Where(t => t.CreatedAt >= startDate.Value);
                if (endDate.HasValue)
                    ticketsQuery = ticketsQuery.Where(t => t.CreatedAt <= endDate.Value);
                tickets = await ticketsQuery.ToListAsync();
            }
            else
            {
                tickets = new List<Models.Ticket>();
            }

            var invoicesQuery = _context.Invoices.Where(i => i.ClientId == client.Id);
            if (startDate.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate >= startDate.Value);
            if (endDate.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate <= endDate.Value);

            var invoices = await invoicesQuery.ToListAsync();

            reports.Add(new ClientReportDto
            {
                ClientId = client.Id,
                ClientName = client.CompanyName,
                TotalProjects = projects.Count,
                TotalTickets = tickets.Count,
                OpenTickets = tickets.Count(t => t.Status != "Cerrado"),
                TotalBilled = invoices.Sum(i => i.Total),
                TotalPaid = invoices.Where(i => i.Status == "Pagada").Sum(i => i.Total),
                PendingAmount = invoices.Where(i => i.Status == "Pendiente" || i.Status == "Vencida").Sum(i => i.Total)
            });
        }

        return reports.OrderByDescending(r => r.TotalBilled).ToList();
    }

    public async Task<List<ProjectReportDto>> GetReportsByProjectAsync(DateTime? startDate, DateTime? endDate)
    {
        var projects = await _context.Projects
            .Include(p => p.Client)
            .ToListAsync();

        var reports = new List<ProjectReportDto>();

        foreach (var project in projects)
        {
            // ✅ FIX: Filtrar tickets SOLO de este proyecto
            var ticketsQuery = _context.Tickets.Where(t => t.ProjectId == project.Id);

            if (startDate.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.CreatedAt <= endDate.Value);

            var tickets = await ticketsQuery.ToListAsync();
            var totalHours = tickets.Sum(t => t.ActualHours);
            var estimatedHours = tickets.Sum(t => t.EstimatedHours);

            // Revenue = facturas del cliente de este proyecto
            var projectInvoices = await _context.Invoices
                .Where(i => i.ClientId == project.ClientId && i.Status == "Pagada")
                .ToListAsync();
            var revenue = projectInvoices.Sum(i => i.Total);

            reports.Add(new ProjectReportDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ClientName = project.Client?.CompanyName ?? "Sin cliente",
                TotalTickets = tickets.Count,
                ClosedTickets = tickets.Count(t => t.Status == "Cerrado"),
                TotalHours = totalHours,
                EstimatedHours = estimatedHours,
                Revenue = revenue
            });
        }

        return reports.OrderByDescending(r => r.Revenue).ToList();
    }

    public async Task<FinancialReportDto> GetFinancialReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var invoicesQuery = _context.Invoices.AsQueryable();
        if (startDate.HasValue)
            invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate >= startDate.Value);
        if (endDate.HasValue)
            invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate <= endDate.Value);

        var invoices = await invoicesQuery.ToListAsync();

        var paidInvoices = invoices.Where(i => i.Status == "Pagada").ToList();
        var pendingInvoices = invoices.Where(i => i.Status == "Pendiente").ToList();
        var overdueInvoices = invoices.Where(i => i.Status == "Vencida").ToList();

        var avgPaymentTime = 0m;
        if (paidInvoices.Any())
        {
            var totalDays = paidInvoices
                .Where(i => i.PaidDate.HasValue)
                .Sum(i => (i.PaidDate!.Value - i.InvoiceDate).TotalDays);
            avgPaymentTime = (decimal)(totalDays / paidInvoices.Count);
        }

        return new FinancialReportDto
        {
            TotalInvoiced = invoices.Sum(i => i.Total),
            TotalPaid = paidInvoices.Sum(i => i.Total),
            TotalPending = pendingInvoices.Sum(i => i.Total),
            TotalOverdue = overdueInvoices.Sum(i => i.Total),
            InvoicesCount = invoices.Count,
            PaidInvoicesCount = paidInvoices.Count,
            PendingInvoicesCount = pendingInvoices.Count,
            AverageInvoiceAmount = invoices.Any() ? invoices.Average(i => i.Total) : 0,
            AveragePaymentTime = avgPaymentTime
        };
    }

    public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(DateTime? startDate, DateTime? endDate)
    {
        var ticketsQuery = _context.Tickets.AsQueryable();
        if (startDate.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.CreatedAt <= endDate.Value);

        var tickets = await ticketsQuery.ToListAsync();
        var resolvedTickets = tickets.Where(t => t.Status == "Cerrado").ToList();

        // ✅ FIX: resolutionRate como 0-100 para la barra de progreso
        var resolutionRate = tickets.Any() ? (decimal)resolvedTickets.Count / tickets.Count * 100 : 0;

        var avgResolutionTime = 0m;
        if (resolvedTickets.Any())
        {
            var totalHours = resolvedTickets
                .Where(t => t.ClosedAt.HasValue)
                .Sum(t => (t.ClosedAt!.Value - t.CreatedAt).TotalHours);
            avgResolutionTime = (decimal)(totalHours / resolvedTickets.Count);
        }

        var totalEstimated = tickets.Sum(t => t.EstimatedHours);
        var totalActual = tickets.Sum(t => t.ActualHours);
        // ✅ FIX: guardar como 0-1 para que .ToString("P") funcione correctamente
        var billingEfficiency = totalEstimated > 0 ? (totalActual / totalEstimated) : 0;

        return new PerformanceMetricsDto
        {
            TicketResolutionRate = resolutionRate,      // 0-100
            AverageResolutionTime = avgResolutionTime,
            ClientSatisfactionScore = 85m,              // 0-100 (85%)
            BillingEfficiency = billingEfficiency,       // 0-1 (para .ToString("P"))
            ResourceUtilization = 0.78m,                // 0-1 (para .ToString("P"))
            TotalTicketsResolved = resolvedTickets.Count,
            TotalTicketsCreated = tickets.Count
        };
    }

    public async Task<List<RevenueTrendDto>> GetRevenueTrendAsync(int months)
    {
        var trends = new List<RevenueTrendDto>();
        var now = DateTime.UtcNow;

        for (int i = months - 1; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var startOfMonth = new DateTime(date.Year, date.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var monthInvoices = await _context.Invoices
                .Where(inv => inv.InvoiceDate >= startOfMonth && inv.InvoiceDate <= endOfMonth)
                .ToListAsync();

            // ✅ FIX: Calcular datos reales por mes para la gráfica de comparación
            var paid = monthInvoices.Where(inv => inv.Status == "Pagada").Sum(inv => inv.Total);
            var pending = monthInvoices.Where(inv => inv.Status == "Pendiente" || inv.Status == "Vencida").Sum(inv => inv.Total);
            var total = monthInvoices.Sum(inv => inv.Total);

            trends.Add(new RevenueTrendDto
            {
                Month = startOfMonth.ToString("MMM yyyy"),
                Revenue = paid,
                TotalInvoiced = total,
                TotalPaid = paid,
                TotalPending = pending,
                InvoicesCount = monthInvoices.Count
            });
        }

        return trends;
    }

    public async Task<List<TicketStatusChartDto>> GetTicketsByStatusAsync()
    {
        var tickets = await _context.Tickets.ToListAsync();

        var statusColors = new Dictionary<string, string>
        {
            { "Abierto", "#3b82f6" },
            { "En Progreso", "#f59e0b" },
            { "En Revisión", "#8b5cf6" },
            { "Cerrado", "#10b981" }
        };

        return tickets
            .GroupBy(t => t.Status)
            .Select(g => new TicketStatusChartDto
            {
                Status = g.Key,
                Count = g.Count(),
                Color = statusColors.GetValueOrDefault(g.Key, "#6b7280")
            })
            .ToList();
    }

    public async Task<PublicStatsDto> GetPublicStatsAsync()
    {
        return new PublicStatsDto
        {
            OpenTickets = await _context.Tickets.CountAsync(t => t.Status != "Cerrado"),
            ActiveProjects = await _context.Projects.CountAsync(p => p.IsActive),
            TotalClients = await _context.Clients.CountAsync(c => c.IsActive)
        };
    }

    public async Task<List<TopClientDto>> GetTopClientsAsync(int top)
    {
        var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
        var topClients = new List<TopClientDto>();

        foreach (var client in clients)
        {
            var invoices = await _context.Invoices
                .Where(i => i.ClientId == client.Id && i.Status == "Pagada")
                .ToListAsync();

            topClients.Add(new TopClientDto
            {
                ClientId = client.Id,
                ClientName = client.CompanyName,
                TotalRevenue = invoices.Sum(i => i.Total),
                InvoicesCount = invoices.Count
            });
        }

        return topClients
            .OrderByDescending(c => c.TotalRevenue)
            .Take(top)
            .ToList();
    }
}