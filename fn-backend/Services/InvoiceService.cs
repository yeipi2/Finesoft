using fs_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace fs_backend.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IPaymentPolicy _paymentPolicy;
    private readonly IReceiptStorageService _receiptStorageService;
    private readonly IInvoiceStatusService _invoiceStatusService;
    private readonly IInvoicePdfGenerator _invoicePdfGenerator;
    private readonly IInvoiceMapper _invoiceMapper;
    private readonly IMonthlyBillingService _monthlyBillingService;
    private readonly IInvoiceNumberService _invoiceNumberService;

    public InvoiceService(ApplicationDbContext context, UserManager<IdentityUser> userManager,
        IPaymentPolicy paymentPolicy,
        IReceiptStorageService receiptStorageService,
        IInvoiceStatusService invoiceStatusService,
        IInvoicePdfGenerator invoicePdfGenerator,
        IInvoiceMapper invoiceMapper,
        IMonthlyBillingService monthlyBillingService,
        IInvoiceNumberService invoiceNumberService)
    {
        _context = context;
        _userManager = userManager;
        _paymentPolicy = paymentPolicy;
        _receiptStorageService = receiptStorageService;
        _invoiceStatusService = invoiceStatusService;
        _invoicePdfGenerator = invoicePdfGenerator;
        _invoiceMapper = invoiceMapper;
        _monthlyBillingService = monthlyBillingService;
        _invoiceNumberService = invoiceNumberService;
    }

    public async Task<IEnumerable<InvoiceDetailDto>> GetInvoicesAsync(string? status = null,
        string? invoiceType = null, int? clientId = null)
    {
        var query = _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Items)
                .ThenInclude(item => item.Ticket)
                    .ThenInclude(t => t!.Project)
                        .ThenInclude(p => p!.Client)
            .Include(i => i.Payments)
            .Include(i => i.Quote)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrEmpty(invoiceType))
        {
            query = query.Where(i => i.InvoiceType == invoiceType);
        }

        if (clientId.HasValue)
        {
            query = query.Where(i => i.ClientId == clientId.Value);
        }

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();

        var invoiceDtos = new List<InvoiceDetailDto>();
        foreach (var invoice in invoices)
        {
            invoiceDtos.Add(await _invoiceMapper.MapToDetailDtoAsync(invoice));
        }

        return invoiceDtos;
    }

    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Items)
                .ThenInclude(item => item.Ticket)
                    .ThenInclude(t => t!.Project)
                        .ThenInclude(p => p!.Client)
            .Include(i => i.Payments)
            .Include(i => i.Quote)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice == null ? null : await _invoiceMapper.MapToDetailDtoAsync(invoice);
    }

    public async Task<ServiceResult<InvoiceDetailDto>> CreateInvoiceAsync(InvoiceDto invoiceDto,
        string createdByUserId)
    {
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == invoiceDto.ClientId);
        if (!clientExists)
        {
            return ServiceResult<InvoiceDetailDto>.Failure("El cliente especificado no existe");
        }

        if (invoiceDto.Items == null || !invoiceDto.Items.Any())
        {
            return ServiceResult<InvoiceDetailDto>.Failure("La factura debe tener al menos un elemento");
        }

        if (!_paymentPolicy.TryNormalizePayType(invoiceDto.PaymentType, out var normalizedPaymentType))
            return ServiceResult<InvoiceDetailDto>.Failure("PaymentType inválido. Usa PUE o PPD.");

        invoiceDto.PaymentType = normalizedPaymentType;

        if (invoiceDto.PaymentType == InvoiceConstants.PaymentType.Pue)
        {
            try
            {
                invoiceDto.PaymentMethod = _paymentPolicy.NormalizePaymentMethodOrThrow(invoiceDto.PaymentMethod);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<InvoiceDetailDto>.Failure(ex.Message);
            }

            if (string.IsNullOrWhiteSpace(invoiceDto.PaymentMethod))
                return ServiceResult<InvoiceDetailDto>.Failure("Para PUE debes especificar el método de pago.");
        }
        else
        {
            invoiceDto.PaymentMethod = string.Empty;
        }

        var invoiceNumber = await _invoiceNumberService.GenerateAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            ClientId = invoiceDto.ClientId,
            QuoteId = invoiceDto.QuoteId,
            InvoiceDate = invoiceDto.InvoiceDate ?? DateTime.UtcNow,
            DueDate = invoiceDto.DueDate,
            InvoiceType = invoiceDto.InvoiceType,
            Status = InvoiceConstants.Status.Pending,
            PaymentType = invoiceDto.PaymentType,
            PaymentMethod = invoiceDto.PaymentMethod ?? string.Empty,
            CreatedByUserId = createdByUserId,
            Notes = invoiceDto.Notes
        };

        decimal subtotal = 0;
        foreach (var itemDto in invoiceDto.Items)
        {
            var itemSubtotal = itemDto.Quantity * itemDto.UnitPrice;
            subtotal += itemSubtotal;

            var item = new InvoiceItem
            {
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Subtotal = itemSubtotal,
                TicketId = itemDto.TicketId
            };

            invoice.Items.Add(item);
        }

        invoice.Subtotal = subtotal;
        invoice.Tax = subtotal * 0.16m;
        invoice.Total = invoice.Subtotal + invoice.Tax;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        await _context.Entry(invoice).Reference(i => i.Client).LoadAsync();
        await _context.Entry(invoice).Collection(i => i.Items).LoadAsync();

        // Cargar relaciones de tickets
        foreach (var item in invoice.Items)
        {
            if (item.TicketId.HasValue)
            {
                await _context.Entry(item)
                    .Reference(i => i.Ticket)
                    .Query()
                    .Include(t => t.Project)
                        .ThenInclude(p => p.Client)
                    .LoadAsync();
            }
        }

        return ServiceResult<InvoiceDetailDto>.Success(await _invoiceMapper.MapToDetailDtoAsync(invoice));
    }

    // ⭐ MÉTODO CORREGIDO: CreateInvoiceFromQuoteAsync
    public async Task<ServiceResult<InvoiceDetailDto>> CreateInvoiceFromQuoteAsync(
        CreateInvoiceFromQuoteDto dto, string createdByUserId)
    {
        // Cargar cotización con TODOS los datos necesarios
        var quote = await _context.Quotes
            .Include(q => q.Client)
            .Include(q => q.Items)
                .ThenInclude(i => i.Ticket)
                    .ThenInclude(t => t!.Project)
                        .ThenInclude(p => p!.Client)
            .FirstOrDefaultAsync(q => q.Id == dto.QuoteId);

        if (quote == null)
        {
            return ServiceResult<InvoiceDetailDto>.Failure("Cotización no encontrada");
        }

        if (quote.Status != "Aceptada")
        {
            return ServiceResult<InvoiceDetailDto>.Failure("Solo se pueden facturar cotizaciones aceptadas");
        }

        // Verificar si ya existe factura para esta cotización
        var existingInvoice = await _context.Invoices.AnyAsync(i => i.QuoteId == dto.QuoteId);
        if (existingInvoice)
        {
            return ServiceResult<InvoiceDetailDto>.Failure("Esta cotización ya tiene una factura asociada");
        }

        if (!_paymentPolicy.TryNormalizePayType(dto.PaymentType, out var normalizedPaymentType))
            return ServiceResult<InvoiceDetailDto>.Failure("PaymentType inválido. Usa PUE o PPD.");

        dto.PaymentType = normalizedPaymentType;

        if (dto.PaymentType == InvoiceConstants.PaymentType.Ppd)
        {
            dto.PaymentMethod = string.Empty;
        }
        else // PUE
        {
            try
            {
                dto.PaymentMethod = _paymentPolicy.NormalizePaymentMethodOrThrow(dto.PaymentMethod);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<InvoiceDetailDto>.Failure(ex.Message);
            }

            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
                return ServiceResult<InvoiceDetailDto>.Failure("Para PUE debes especificar el método de pago.");
        }

        var invoiceNumber = await _invoiceNumberService.GenerateAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            ClientId = quote.ClientId,
            QuoteId = quote.Id,
            InvoiceDate = dto.InvoiceDate ?? DateTime.UtcNow,
            DueDate = dto.DueDate,
            InvoiceType = InvoiceConstants.InvoiceType.Event,
            Status = InvoiceConstants.Status.Pending,
            PaymentType = dto.PaymentType,
            PaymentMethod = dto.PaymentMethod ?? string.Empty,
            CreatedByUserId = createdByUserId,
            Subtotal = quote.Subtotal,
            Tax = quote.Tax,
            Total = quote.Total,
            Notes = dto.Notes ?? quote.Notes
        };

        // ⭐ CRÍTICO: Copiar items CON TicketId
        foreach (var quoteItem in quote.Items)
        {
            var invoiceItem = new InvoiceItem
            {
                Description = quoteItem.Description,
                Quantity = quoteItem.Quantity,
                UnitPrice = quoteItem.UnitPrice,
                Subtotal = quoteItem.Subtotal,
                TicketId = quoteItem.TicketId // ⭐ ESTO ES ESENCIAL
            };

            invoice.Items.Add(invoiceItem);
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Cargar relaciones completas
        await _context.Entry(invoice).Reference(i => i.Client).LoadAsync();
        await _context.Entry(invoice).Collection(i => i.Items).LoadAsync();

        foreach (var item in invoice.Items)
        {
            if (item.TicketId.HasValue)
            {
                await _context.Entry(item)
                    .Reference(i => i.Ticket)
                    .Query()
                    .Include(t => t.Project)
                        .ThenInclude(p => p.Client)
                    .LoadAsync();
            }
        }

        return ServiceResult<InvoiceDetailDto>.Success(await _invoiceMapper.MapToDetailDtoAsync(invoice));
    }

    public async Task<ServiceResult<InvoiceDetailDto>> UpdateInvoiceAsync(int id, InvoiceDto invoiceDto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return ServiceResult<InvoiceDetailDto>.Failure("Factura no encontrada");
        }

        var clientExists = await _context.Clients.AnyAsync(c => c.Id == invoiceDto.ClientId);
        if (!clientExists)
        {
            return ServiceResult<InvoiceDetailDto>.Failure("El cliente especificado no existe");
        }

        if (invoiceDto.Items == null || !invoiceDto.Items.Any())
        {
            return ServiceResult<InvoiceDetailDto>.Failure("La factura debe tener al menos un elemento");
        }

        if (invoice.Status is InvoiceConstants.Status.Cancelled or InvoiceConstants.Status.Paid)
        {
            return ServiceResult<InvoiceDetailDto>.Failure(
                $"No se puede editar una factura {invoice.Status.ToLower()}");
        }


        if (!_paymentPolicy.TryNormalizePayType(invoiceDto.PaymentType, out var normalizedPaymentType))
            return ServiceResult<InvoiceDetailDto>.Failure("PaymentType inválido. Usa PUE o PPD.");

        invoiceDto.PaymentType = normalizedPaymentType;

        if (invoiceDto.PaymentType == InvoiceConstants.PaymentType.Pue)
        {
            try
            {
                invoiceDto.PaymentMethod = _paymentPolicy.NormalizePaymentMethodOrThrow(invoiceDto.PaymentMethod);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<InvoiceDetailDto>.Failure(ex.Message);
            }

            if (string.IsNullOrWhiteSpace(invoiceDto.PaymentMethod))
                return ServiceResult<InvoiceDetailDto>.Failure("Para PUE debes especificar el método de pago.");
        }
        else
        {
            invoiceDto.PaymentMethod = string.Empty;
        }

        invoice.ClientId = invoiceDto.ClientId;
        invoice.InvoiceDate = invoiceDto.InvoiceDate ?? invoice.InvoiceDate;
        invoice.DueDate = invoiceDto.DueDate;
        invoice.InvoiceType = invoiceDto.InvoiceType;
        invoice.Status = invoiceDto.Status;
        invoice.PaymentType = invoiceDto.PaymentType;
        invoice.PaymentMethod = invoiceDto.PaymentMethod ?? string.Empty;
        invoice.Notes = invoiceDto.Notes;

        _context.Set<InvoiceItem>().RemoveRange(invoice.Items);

        decimal subtotal = 0;
        invoice.Items.Clear();

        foreach (var itemDto in invoiceDto.Items)
        {
            var itemSubtotal = itemDto.Quantity * itemDto.UnitPrice;
            subtotal += itemSubtotal;

            var item = new InvoiceItem
            {
                InvoiceId = id,
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Subtotal = itemSubtotal,
                TicketId = itemDto.TicketId
            };

            invoice.Items.Add(item);
        }

        invoice.Subtotal = subtotal;
        invoice.Tax = subtotal * 0.16m;
        invoice.Total = invoice.Subtotal + invoice.Tax;

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();

        await _context.Entry(invoice).Reference(i => i.Client).LoadAsync();
        await _context.Entry(invoice).Collection(i => i.Items).LoadAsync();

        foreach (var item in invoice.Items)
        {
            if (item.TicketId.HasValue)
            {
                await _context.Entry(item)
                    .Reference(i => i.Ticket)
                    .Query()
                    .Include(t => t.Project)
                        .ThenInclude(p => p.Client)
                    .LoadAsync();
            }
        }

        return ServiceResult<InvoiceDetailDto>.Success(await _invoiceMapper.MapToDetailDtoAsync(invoice));
    }

    public async Task<ServiceResult<bool>> DeleteInvoiceAsync(int id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
        {
            return ServiceResult<bool>.Failure("Factura no encontrada");
        }

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ChangeInvoiceStatusAsync(int id, string newStatus, string? reason)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return ServiceResult<bool>.Failure("Factura no encontrada");

        if (invoice.Status is InvoiceConstants.Status.Cancelled or InvoiceConstants.Status.Paid)
            return ServiceResult<bool>.Failure("La factura no puede modificarse en este estado");

        var validStatuses = new[]
        {
            InvoiceConstants.Status.Pending,
            InvoiceConstants.Status.Paid,
            InvoiceConstants.Status.Cancelled
        };
        if (!validStatuses.Contains(newStatus))
            return ServiceResult<bool>.Failure("Estado no válido");

        if (newStatus == InvoiceConstants.Status.Cancelled)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return ServiceResult<bool>.Failure("Debes especificar el motivo de cancelación");

            invoice.CancellationReason = reason.Trim();
            invoice.CancelledDate = DateTime.UtcNow;
        }

        if (newStatus == InvoiceConstants.Status.Paid)
        {
            invoice.PaidDate = DateTime.UtcNow;
        }

        invoice.Status = newStatus;

        await _context.SaveChangesAsync();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<InvoicePaymentDto>> AddPaymentAsync(
    int invoiceId,
    RegisterInvoicePaymentDto dto,
    string userId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return ServiceResult<InvoicePaymentDto>.Failure("Factura no encontrada");

        if (invoice.Status is InvoiceConstants.Status.Cancelled or InvoiceConstants.Status.Paid)
            return ServiceResult<InvoicePaymentDto>.Failure("La factura no puede registrar pagos en este estado");

        var validation = _paymentPolicy.ValidateInvoicePayment(invoice, dto.Amount, dto.PaymentMethod);
        if (!validation.Succeeded)
            return ServiceResult<InvoicePaymentDto>.Failure(validation.Errors);

        var paymentInfo = validation.Data!;
        dto.PaymentMethod = paymentInfo.PaymentMethod;

        var receiptResult = await _receiptStorageService.SaveOptionalAsync(invoiceId, dto.Receipt);
        if (!receiptResult.Succeeded)
            return ServiceResult<InvoicePaymentDto>.Failure(receiptResult.Errors);

        var receipt = receiptResult.Data;

        var payment = new InvoicePayment
        {
            InvoiceId = invoiceId,
            Amount = paymentInfo.Amount,
            PaymentDate = dto.PaymentDate,
            PaymentMethod = dto.PaymentMethod,
            Reference = dto.Reference ?? string.Empty,
            Notes = dto.Notes ?? string.Empty,
            RecordedByUserId = userId,

            ReceiptPath = receipt?.Path,
            ReceiptFileName = receipt?.FileName,
            ReceiptContentType = receipt?.ContentType,
            ReceiptSize = receipt?.Size,
            ReceiptUploadedAt = receipt?.UploadedAt
        };

        _context.InvoicePayments.Add(payment);

        var newBalance = paymentInfo.BalanceBefore - paymentInfo.Amount;
        _invoiceStatusService.ApplyAfterPayment(invoice, newBalance);

        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);

        return ServiceResult<InvoicePaymentDto>.Success(new InvoicePaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            Reference = payment.Reference,
            Notes = payment.Notes,
            RecordedByUserId = userId,
            RecordedByUserName = user?.UserName,

            ReceiptPath = payment.ReceiptPath,
            ReceiptFileName = payment.ReceiptFileName,
            ReceiptContentType = payment.ReceiptContentType,
            ReceiptSize = payment.ReceiptSize
        });
    }


    public async Task<ServiceResult<InvoicePaymentDto>> AddPaymentWithReceiptAsync(
    int invoiceId,
    AddInvoicePaymentWithReceiptRequest request,
    string userId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return ServiceResult<InvoicePaymentDto>.Failure("Factura no encontrada");

        if (invoice.Status is InvoiceConstants.Status.Cancelled or InvoiceConstants.Status.Paid)
            return ServiceResult<InvoicePaymentDto>.Failure("No se puede registrar pago en este estado");

        var validation = _paymentPolicy.ValidateInvoicePayment(invoice, request.Amount, request.PaymentMethod);
        if (!validation.Succeeded)
            return ServiceResult<InvoicePaymentDto>.Failure(validation.Errors);

        var paymentInfo = validation.Data!;
        request.PaymentMethod = paymentInfo.PaymentMethod;

        var receiptResult = await _receiptStorageService.SaveRequiredAsync(invoiceId, request.Receipt);
        if (!receiptResult.Succeeded)
            return ServiceResult<InvoicePaymentDto>.Failure(receiptResult.Errors);

        var receipt = receiptResult.Data!;

        var payment = new InvoicePayment
        {
            InvoiceId = invoiceId,
            Amount = paymentInfo.Amount,
            PaymentDate = request.PaymentDate,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            RecordedByUserId = userId,

            ReceiptFileName = receipt.FileName,
            ReceiptContentType = receipt.ContentType,
            ReceiptSize = receipt.Size,
            ReceiptPath = receipt.Path,
            ReceiptUploadedAt = receipt.UploadedAt
        };

        _context.InvoicePayments.Add(payment);

        var newBalance = paymentInfo.BalanceBefore - paymentInfo.Amount;
        _invoiceStatusService.ApplyAfterPayment(invoice, newBalance);

        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);

        return ServiceResult<InvoicePaymentDto>.Success(new InvoicePaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            Reference = payment.Reference,
            Notes = payment.Notes,
            RecordedByUserId = userId,
            RecordedByUserName = user?.UserName,

            ReceiptPath = payment.ReceiptPath,
            ReceiptFileName = payment.ReceiptFileName,
            ReceiptContentType = payment.ReceiptContentType,
            ReceiptSize = payment.ReceiptSize
        });
    }


    public Task<ServiceResult<bool>> GenerateMonthlyInvoicesAsync(string userId, List<int>? clientIds = null)
    {
        return _monthlyBillingService.GenerateMonthlyInvoicesAsync(userId, clientIds);
    }

    public Task<List<MonthlyClientSummaryDto>> GetMonthlySummaryAsync()
    {
        return _monthlyBillingService.GetMonthlySummaryAsync();
    }

    public async Task<InvoiceStatsDto> GetInvoiceStatsAsync()
    {
        var invoices = await _context.Invoices.ToListAsync();

        var stats = new InvoiceStatsDto
        {
            PaidInvoices = invoices.Count(i => i.Status == InvoiceConstants.Status.Paid),
            TotalPaid = invoices.Where(i => i.Status == InvoiceConstants.Status.Paid).Sum(i => (decimal?)i.Total) ?? 0,
            PendingInvoices = invoices.Count(i => i.Status == InvoiceConstants.Status.Pending),
            TotalPending = invoices.Where(i => i.Status == InvoiceConstants.Status.Pending).Sum(i => (decimal?)i.Total) ?? 0,
            OverdueInvoices = invoices.Count(i => i.Status == InvoiceConstants.Status.Overdue),
            TotalOverdue = invoices.Where(i => i.Status == InvoiceConstants.Status.Overdue).Sum(i => (decimal?)i.Total) ?? 0,
            TotalInvoices = invoices.Count,
            TotalBilled = invoices.Sum(i => (decimal?)i.Total) ?? 0
        };

        return stats;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Items)
                .ThenInclude(item => item.Ticket)
                    .ThenInclude(t => t!.Project)
                        .ThenInclude(p => p!.Client)
            .Include(i => i.Payments)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            throw new Exception("Factura no encontrada");

        var user = await _userManager.FindByIdAsync(invoice.CreatedByUserId);
        return _invoicePdfGenerator.Generate(invoice, user?.UserName ?? "Sistema");
    }

    public async Task<List<int>> GetTicketsInUseAsync()
    {
        return await _context.InvoiceItems
            .Where(ii => ii.TicketId.HasValue)
            .Select(ii => ii.TicketId!.Value)
            .Distinct()
            .ToListAsync();
    }
}
