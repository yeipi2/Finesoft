// fs-backend/Services/MonthlyBillingService.cs  — COMPLETO ACTUALIZADO
using fn_backend.Models;
using fs_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class MonthlyBillingService : IMonthlyBillingService
{
    private readonly ApplicationDbContext _context;
    private readonly IInvoiceNumberService _invoiceNumberService;
    private readonly ILogger<MonthlyBillingService> _logger;

    public MonthlyBillingService(
        ApplicationDbContext context,
        IInvoiceNumberService invoiceNumberService,
        ILogger<MonthlyBillingService> logger)
    {
        _context = context;
        _invoiceNumberService = invoiceNumberService;
        _logger = logger;
    }

    // ⭐ ACTUALIZADO: cada cliente lleva su propio PaymentMethod y PaymentForm
    public async Task<ServiceResult<bool>> GenerateMonthlyInvoicesAsync(
        string userId, List<GenerateMonthlyInvoiceItemDto> items)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);

        var clientIds = items.Select(i => i.ClientId).ToList();

        var clients = await _context.Clients
            .Where(c => (c.BillingFrequency == "Monthly" || c.ServiceMode == "Mensual")
                        && c.IsActive
                        && clientIds.Contains(c.Id))
            .ToListAsync();

        if (!clients.Any())
            return ServiceResult<bool>.Failure("No se encontraron clientes con facturación mensual activa");

        var invoicesCreated = 0;

        foreach (var client in clients)
        {
            // Obtener el item con los datos de pago elegidos para este cliente
            var itemDto = items.FirstOrDefault(i => i.ClientId == client.Id);
            if (itemDto == null) continue;

            var paymentType = itemDto.PaymentMethod.ToUpper() == "PUE"
                                    ? InvoiceConstants.PaymentType.Pue
                                    : InvoiceConstants.PaymentType.Ppd;

            // Para PUE la forma de pago es requerida; para PPD se deja vacía
            var paymentMethod = paymentType == InvoiceConstants.PaymentType.Pue
                                    ? (itemDto.PaymentForm ?? string.Empty)
                                    : string.Empty;

            var existingActiveInvoice = await _context.Invoices
                .AnyAsync(i => i.ClientId == client.Id
                            && i.InvoiceType == InvoiceConstants.InvoiceType.Monthly
                            && i.InvoiceDate.Month == currentMonth.Month
                            && i.InvoiceDate.Year == currentMonth.Year
                            && i.Status != InvoiceConstants.Status.Cancelled);

            if (existingActiveInvoice)
            {
                _logger.LogInformation("Saltando {CompanyName} - ya tiene factura activa este mes", client.CompanyName);
                continue;
            }

            var projectIds = await _context.Projects
                .Where(p => p.ClientId == client.Id)
                .Select(p => p.Id)
                .ToListAsync();

            var ticketsThisMonth = new List<Ticket>();
            decimal hoursUsed = 0;

            if (projectIds.Any())
            {
                ticketsThisMonth = await _context.Tickets
                    .Where(t => t.ProjectId.HasValue
                             && projectIds.Contains(t.ProjectId.Value)
                             && t.UpdatedAt >= currentMonth
                             && t.UpdatedAt < nextMonth)
                    .ToListAsync();

                hoursUsed = ticketsThisMonth.Sum(t => t.ActualHours);
            }

            var monthlyHours = client.MonthlyHours;
            var subtotal = (client.MonthlyRate.HasValue && client.MonthlyRate.Value > 0)
                                   ? (decimal)client.MonthlyRate.Value
                                   : 0m;

            var notes = $"Factura mensual - {currentMonth.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-MX"))}";
            if (monthlyHours > 0 && hoursUsed > monthlyHours + 10)
            {
                var excess = hoursUsed - monthlyHours;
                notes += $"\n⚠ AVISO: Este cliente excedió {excess:0.0}h sobre las {monthlyHours}h incluidas " +
                         "en su póliza. Se recomienda revisar y renegociar el costo de la póliza para el siguiente mes.";
            }

            var hoursInfo = string.Empty;
            if (monthlyHours > 0)
            {
                var excess = hoursUsed - monthlyHours;
                if (excess > 10)
                    hoursInfo = $" | {monthlyHours}h incluidas | {hoursUsed}h usadas | ⚠ +{excess:0.0}h excedido";
                else if (excess > 0)
                    hoursInfo = $" | {monthlyHours}h incluidas | {hoursUsed}h usadas | +{excess:0.0}h excedido (margen aceptable)";
                else
                    hoursInfo = $" | {monthlyHours}h incluidas | {hoursUsed}h usadas";
            }

            var invoiceNumber = await _invoiceNumberService.GenerateAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                ClientId = client.Id,
                InvoiceDate = currentMonth,
                DueDate = currentMonth.AddDays(30),
                InvoiceType = InvoiceConstants.InvoiceType.Monthly,
                Status = InvoiceConstants.Status.Pending,
                PaymentType = paymentType,    // ⭐ viene del selector por cliente
                PaymentMethod = paymentMethod,  // ⭐ vacío para PPD, forma de pago para PUE
                CreatedByUserId = userId,
                Notes = notes
            };

            invoice.Items.Add(new InvoiceItem
            {
                Description = $"Póliza mensual de servicios - {currentMonth.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-MX"))}{hoursInfo}",
                Quantity = 1,
                UnitPrice = subtotal,
                Subtotal = subtotal
            });

            foreach (var ticket in ticketsThisMonth)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Description = $"Ticket #{ticket.Id} — {ticket.Title} ({ticket.ActualHours:0.0}h)",
                    Quantity = 1,
                    UnitPrice = 0m,
                    Subtotal = 0m,
                    TicketId = ticket.Id
                });
            }

            invoice.Subtotal = subtotal;
            invoice.Tax = subtotal * 0.16m;
            invoice.Total = invoice.Subtotal + invoice.Tax;

            _context.Invoices.Add(invoice);
            invoicesCreated++;

            _logger.LogInformation(
                "Factura mensual generada para {CompanyName} | {PaymentType} | {PaymentMethod} | {TicketCount} tickets",
                client.CompanyName, paymentType, paymentMethod, ticketsThisMonth.Count);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Total facturas mensuales generadas: {Count}", invoicesCreated);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<List<MonthlyClientSummaryDto>> GetMonthlySummaryAsync()
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);

        var clients = await _context.Clients
            .Where(c => (c.BillingFrequency == "Monthly" || c.ServiceMode == "Mensual") && c.IsActive)
            .ToListAsync();

        var result = new List<MonthlyClientSummaryDto>();

        foreach (var client in clients)
        {
            var projectIds = await _context.Projects
                .Where(p => p.ClientId == client.Id)
                .Select(p => p.Id)
                .ToListAsync();

            var tickets = new List<MonthlyTicketSummaryDto>();
            decimal hoursUsed = 0;

            if (projectIds.Any())
            {
                var rawTickets = await _context.Tickets
                    .Include(t => t.Project)
                    .Where(t => t.ProjectId.HasValue
                             && projectIds.Contains(t.ProjectId.Value)
                             && t.UpdatedAt >= currentMonth
                             && t.UpdatedAt < nextMonth)
                    .OrderByDescending(t => t.ActualHours)
                    .ToListAsync();

                hoursUsed = rawTickets.Sum(t => t.ActualHours);

                tickets = rawTickets.Select(t => new MonthlyTicketSummaryDto
                {
                    TicketId = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    ProjectName = t.Project?.Name ?? string.Empty,
                    ActualHours = t.ActualHours,
                    UpdatedAt = t.UpdatedAt
                }).ToList();
            }

            var invoicesThisMonth = await _context.Invoices
                .Where(i => i.ClientId == client.Id
                         && i.InvoiceType == InvoiceConstants.InvoiceType.Monthly
                         && i.InvoiceDate.Month == currentMonth.Month
                         && i.InvoiceDate.Year == currentMonth.Year)
                .ToListAsync();

            var alreadyHasInvoice = invoicesThisMonth.Any(i => i.Status != InvoiceConstants.Status.Cancelled);
            var hasCancelledInvoice = invoicesThisMonth.Any(i => i.Status == InvoiceConstants.Status.Cancelled);

            var monthlyHours = client.MonthlyHours;
            var excess = hoursUsed - monthlyHours;

            string status;
            if (monthlyHours <= 0)
                status = InvoiceConstants.MonthlyHoursStatus.Ok;
            else if (excess > 10)
                status = InvoiceConstants.MonthlyHoursStatus.Critical;
            else if (excess > 0)
                status = InvoiceConstants.MonthlyHoursStatus.Exceeded;
            else if (monthlyHours > 0 && hoursUsed / monthlyHours >= 0.8m)
                status = InvoiceConstants.MonthlyHoursStatus.Warning;
            else
                status = InvoiceConstants.MonthlyHoursStatus.Ok;

            result.Add(new MonthlyClientSummaryDto
            {
                ClientId = client.Id,
                CompanyName = client.CompanyName,
                MonthlyRate = client.MonthlyRate.HasValue ? (decimal)client.MonthlyRate.Value : 0m,
                MonthlyHours = monthlyHours,
                HoursUsed = hoursUsed,
                AlreadyHasInvoice = alreadyHasInvoice,
                HasCancelledInvoice = hasCancelledInvoice,
                HoursStatus = status,
                Tickets = tickets
            });
        }

        return result
            .OrderBy(r => r.AlreadyHasInvoice ? 1 : 0)
            .ThenBy(r => r.HoursStatus switch
            {
                InvoiceConstants.MonthlyHoursStatus.Critical => 0,
                InvoiceConstants.MonthlyHoursStatus.Exceeded => 1,
                InvoiceConstants.MonthlyHoursStatus.Warning => 2,
                _ => 3
            })
            .ToList();
    }
}