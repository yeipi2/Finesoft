using fs_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;


namespace fs_backend.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private static decimal Round2(decimal value)
    => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool IsZero(decimal value)
        => Math.Abs(value) < 0.01m;

    private static string NormalizePayType(string? value)
    {
        var v = (value ?? "PPD").Trim().ToUpperInvariant();
        return (v is "PUE" or "PPD") ? v : "PPD";
    }

    private static readonly Dictionary<string, string> PaymentMethodMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
        // entradas “humanas”
        ["transferencia"] = "Transferencia",
        ["efectivo"] = "Efectivo",
        ["tarjeta"] = "Tarjeta",
        ["cheque"] = "Cheque",
        ["deposito"] = "Depósito",
        ["depósito"] = "Depósito",

        // entradas canónicas (por si ya vienen así)
        ["Transferencia"] = "Transferencia",
        ["Efectivo"] = "Efectivo",
        ["Tarjeta"] = "Tarjeta",
        ["Cheque"] = "Cheque",
        ["Depósito"] = "Depósito",
        ["Deposito"] = "Depósito",
    };

    private static string NormalizePaymentMethodOrThrow(string? value)
    {
        var raw = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        // Opcional: tolerar un punto al final
        raw = raw.TrimEnd('.');

        if (PaymentMethodMap.TryGetValue(raw, out var canonical))
            return canonical;

        throw new ArgumentException("PaymentMethod inválido. Usa: Transferencia, Efectivo, Tarjeta, Cheque o Depósito.");
    }

    public InvoiceService(ApplicationDbContext context, UserManager<IdentityUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
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
            invoiceDtos.Add(await MapToDetailDto(invoice));
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
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice == null ? null : await MapToDetailDto(invoice);
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

        // ✅ Normalizar PaymentType
        invoiceDto.PaymentType = string.IsNullOrWhiteSpace(invoiceDto.PaymentType)
            ? "PPD"
            : invoiceDto.PaymentType.Trim().ToUpperInvariant();

        if (invoiceDto.PaymentType is not ("PUE" or "PPD"))
            return ServiceResult<InvoiceDetailDto>.Failure("PaymentType inválido. Usa PUE o PPD.");

        // ✅ Reglas: PUE requiere método. PPD lo define en pagos
        if (invoiceDto.PaymentType == "PUE" && string.IsNullOrWhiteSpace(invoiceDto.PaymentMethod))
            return ServiceResult<InvoiceDetailDto>.Failure("Para PUE debes especificar el método de pago.");

        if (invoiceDto.PaymentType == "PPD")
            invoiceDto.PaymentMethod = string.Empty;

        var invoiceNumber = await GenerateInvoiceNumber();

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            ClientId = invoiceDto.ClientId,
            QuoteId = invoiceDto.QuoteId,
            InvoiceDate = invoiceDto.InvoiceDate ?? DateTime.UtcNow,
            DueDate = invoiceDto.DueDate,
            InvoiceType = invoiceDto.InvoiceType,
            Status = invoiceDto.Status,
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

        return ServiceResult<InvoiceDetailDto>.Success(await MapToDetailDto(invoice));
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

        dto.PaymentType = string.IsNullOrWhiteSpace(dto.PaymentType)
    ? "PPD"
    : dto.PaymentType.Trim().ToUpperInvariant();

        if (dto.PaymentType is not ("PUE" or "PPD"))
            return ServiceResult<InvoiceDetailDto>.Failure("PaymentType inválido. Usa PUE o PPD.");

        if (dto.PaymentType == "PPD")
        {
            dto.PaymentMethod = string.Empty;
        }
        else // PUE
        {
            try
            {
                dto.PaymentMethod = NormalizePaymentMethodOrThrow(dto.PaymentMethod);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<InvoiceDetailDto>.Failure(ex.Message);
            }

            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
                return ServiceResult<InvoiceDetailDto>.Failure("Para PUE debes especificar el método de pago.");
        }

        var invoiceNumber = await GenerateInvoiceNumber();

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            ClientId = quote.ClientId,
            QuoteId = quote.Id,
            InvoiceDate = dto.InvoiceDate ?? DateTime.UtcNow,
            DueDate = dto.DueDate,
            InvoiceType = "Event",
            Status = "Pendiente",
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

        return ServiceResult<InvoiceDetailDto>.Success(await MapToDetailDto(invoice));
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

        if (invoice.Status is "Cancelada" or "Pagada")
        {
            return ServiceResult<InvoiceDetailDto>.Failure(
                $"No se puede editar una factura {invoice.Status.ToLower()}");
        }


        // ✅ Normalizar PaymentType
        invoiceDto.PaymentType = string.IsNullOrWhiteSpace(invoiceDto.PaymentType)
            ? "PPD"
            : invoiceDto.PaymentType.Trim().ToUpperInvariant();

        if (invoiceDto.PaymentType is not ("PUE" or "PPD"))
            return ServiceResult<InvoiceDetailDto>.Failure("PaymentType inválido. Usa PUE o PPD.");

        // ✅ Reglas: PUE requiere método. PPD lo define en pagos
        if (invoiceDto.PaymentType == "PUE" && string.IsNullOrWhiteSpace(invoiceDto.PaymentMethod))
            return ServiceResult<InvoiceDetailDto>.Failure("Para PUE debes especificar el método de pago.");

        if (invoiceDto.PaymentType == "PPD")
            invoiceDto.PaymentMethod = string.Empty;

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

        return ServiceResult<InvoiceDetailDto>.Success(await MapToDetailDto(invoice));
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

        if (invoice.Status is "Cancelada" or "Pagada")
            return ServiceResult<bool>.Failure("La factura no puede modificarse en este estado");

        var validStatuses = new[] { "Pendiente", "Pagada", "Cancelada" };
        if (!validStatuses.Contains(newStatus))
            return ServiceResult<bool>.Failure("Estado no válido");

        if (newStatus == "Cancelada")
        {
            if (string.IsNullOrWhiteSpace(reason))
                return ServiceResult<bool>.Failure("Debes especificar el motivo de cancelación");

            invoice.CancellationReason = reason.Trim();
            invoice.CancelledDate = DateTime.UtcNow;
        }

        if (newStatus == "Pagada")
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

        if (invoice.Status is "Cancelada" or "Pagada")
            return ServiceResult<InvoicePaymentDto>.Failure("La factura no puede registrar pagos en este estado");

        // ✅ Normaliza montos
        var total = Round2(invoice.Total);
        var alreadyPaid = Round2(invoice.Payments.Sum(p => p.Amount));
        var balance = Round2(total - alreadyPaid);

        if (balance <= 0)
            return ServiceResult<InvoicePaymentDto>.Failure("Esta factura ya está saldada");

        var amount = Round2(dto.Amount);
        if (amount <= 0)
            return ServiceResult<InvoicePaymentDto>.Failure("El monto debe ser mayor a 0");

        // ✅ Reglas PUE/PPD
        var paymentType = NormalizePayType(invoice.PaymentType);

        if (paymentType == "PUE")
        {
            // Debe liquidar exactamente el saldo
            if (!IsZero(balance - amount))
                return ServiceResult<InvoicePaymentDto>.Failure($"Para PUE el monto debe ser exactamente {balance:N2}");

            // Método viene de la factura (si está definido)
            if (!string.IsNullOrWhiteSpace(invoice.PaymentMethod))
            {
                dto.PaymentMethod = invoice.PaymentMethod;
            }

            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
                return ServiceResult<InvoicePaymentDto>.Failure("Para PUE debes tener método de pago definido (en factura o en el pago)");
        }
        else // PPD
        {
            // No permitir exceder saldo
            if (amount > balance)
                return ServiceResult<InvoicePaymentDto>.Failure($"No puedes registrar un pago mayor al saldo pendiente ({balance:N2})");

            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
                return ServiceResult<InvoicePaymentDto>.Failure("El método de pago es obligatorio");
        }

        try
        {
            dto.PaymentMethod = NormalizePaymentMethodOrThrow(dto.PaymentMethod);
        }
        catch (ArgumentException ex)
        {
            return ServiceResult<InvoicePaymentDto>.Failure(ex.Message);
        }

        // ✅ Validar archivo (si viene)
        string? receiptPath = null;
        string? receiptFileName = null;
        string? receiptContentType = null;
        long? receiptSize = null;

        if (dto.Receipt != null && dto.Receipt.Length > 0)
        {
            var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
            if (!allowed.Contains(dto.Receipt.ContentType))
                return ServiceResult<InvoicePaymentDto>.Failure("Tipo de archivo no permitido (solo PDF/JPG/PNG)");

            var folderRelative = Path.Combine("uploads", "invoices", invoiceId.ToString(), "payments");
            var folderPhysical = Path.Combine(_environment.WebRootPath, folderRelative);

            Directory.CreateDirectory(folderPhysical);

            var ext = Path.GetExtension(dto.Receipt.FileName);
            var safeExt = string.IsNullOrWhiteSpace(ext) ? ".bin" : ext.ToLowerInvariant();

            var storedFileName = $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{safeExt}";
            var filePhysicalPath = Path.Combine(folderPhysical, storedFileName);

            await using (var stream = new FileStream(filePhysicalPath, FileMode.Create))
            {
                await dto.Receipt.CopyToAsync(stream);
            }

            receiptPath = Path.Combine(folderRelative, storedFileName).Replace("\\", "/");
            receiptFileName = dto.Receipt.FileName;
            receiptContentType = dto.Receipt.ContentType;
            receiptSize = dto.Receipt.Length;
        }

        var payment = new InvoicePayment
        {
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentDate = dto.PaymentDate,
            PaymentMethod = dto.PaymentMethod,
            Reference = dto.Reference,
            Notes = dto.Notes,
            RecordedByUserId = userId,

            ReceiptPath = receiptPath,
            ReceiptFileName = receiptFileName,
            ReceiptContentType = receiptContentType,
            ReceiptSize = receiptSize,
            ReceiptUploadedAt = receiptPath != null ? DateTime.UtcNow : null
        };

        _context.InvoicePayments.Add(payment);

        // ✅ Recalcular y actualizar status
        var newPaid = Round2(alreadyPaid + amount);
        var newBalance = Round2(total - newPaid);

        if (newBalance <= 0 || IsZero(newBalance))
        {
            invoice.Status = "Pagada";
            invoice.PaidDate ??= DateTime.UtcNow;
        }
        else
        {
            // Si quieres conservar Vencida:
            // invoice.Status = invoice.DueDate.HasValue && invoice.DueDate.Value.Date < DateTime.UtcNow.Date ? "Vencida" : "Pendiente";
            invoice.Status = "Pendiente";
            invoice.PaidDate = null;
        }

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

        if (invoice.Status is "Cancelada" or "Pagada")
            return ServiceResult<InvoicePaymentDto>.Failure("No se puede registrar pago en este estado");

        var total = Round2(invoice.Total);
        var alreadyPaid = Round2(invoice.Payments.Sum(p => p.Amount));
        var balance = Round2(total - alreadyPaid);

        if (balance <= 0)
            return ServiceResult<InvoicePaymentDto>.Failure("Esta factura ya está saldada");

        var amount = Round2(request.Amount);
        if (amount <= 0)
            return ServiceResult<InvoicePaymentDto>.Failure("El monto debe ser mayor a 0");

        // ✅ Reglas PUE/PPD
        var paymentType = NormalizePayType(invoice.PaymentType);

        if (paymentType == "PUE")
        {
            if (!IsZero(balance - amount))
                return ServiceResult<InvoicePaymentDto>.Failure($"Para PUE el monto debe ser exactamente {balance:N2}");

            if (!string.IsNullOrWhiteSpace(invoice.PaymentMethod))
            {
                request.PaymentMethod = invoice.PaymentMethod;
            }

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return ServiceResult<InvoicePaymentDto>.Failure("Para PUE debes tener método de pago definido (en factura o en el pago)");
        }
        else // PPD
        {
            if (amount > balance)
                return ServiceResult<InvoicePaymentDto>.Failure($"No puedes registrar un pago mayor al saldo pendiente ({balance:N2})");

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return ServiceResult<InvoicePaymentDto>.Failure("El método de pago es obligatorio");
        }

        request.PaymentMethod = NormalizePaymentMethodOrThrow(request.PaymentMethod);

        // ✅ Validar archivo (obligatorio aquí)
        var file = request.Receipt;
        if (file == null || file.Length == 0)
            return ServiceResult<InvoicePaymentDto>.Failure("Debes subir un comprobante");

        var allowed = file.ContentType.StartsWith("image/") || file.ContentType == "application/pdf";
        if (!allowed)
            return ServiceResult<InvoicePaymentDto>.Failure("Solo se permite PDF o imagen");

        if (file.Length > 10 * 1024 * 1024)
            return ServiceResult<InvoicePaymentDto>.Failure("El archivo excede 10MB");

        // Guardar archivo en wwwroot/receipts/invoices/{invoiceId}/...
        var folder = Path.Combine(_environment.WebRootPath, "receipts", "invoices", invoiceId.ToString());
        Directory.CreateDirectory(folder);

        var safeExt = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid():N}{safeExt}";
        var storedPath = Path.Combine(folder, storedName);

        await using (var stream = File.Create(storedPath))
            await file.CopyToAsync(stream);

        var relativePath = $"/receipts/invoices/{invoiceId}/{storedName}";

        var payment = new InvoicePayment
        {
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentDate = request.PaymentDate,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            Notes = request.Notes,
            RecordedByUserId = userId,

            ReceiptFileName = file.FileName,
            ReceiptContentType = file.ContentType,
            ReceiptSize = file.Length,
            ReceiptPath = relativePath,
            ReceiptUploadedAt = DateTime.UtcNow
        };

        _context.InvoicePayments.Add(payment);

        // ✅ Recalcular y actualizar status
        var newPaid = Round2(alreadyPaid + amount);
        var newBalance = Round2(total - newPaid);

        if (newBalance <= 0 || IsZero(newBalance))
        {
            invoice.Status = "Pagada";
            invoice.PaidDate ??= DateTime.UtcNow;
        }
        else
        {
            // Si quieres conservar Vencida:
            // invoice.Status = invoice.DueDate.HasValue && invoice.DueDate.Value.Date < DateTime.UtcNow.Date ? "Vencida" : "Pendiente";
            invoice.Status = "Pendiente";
            invoice.PaidDate = null;
        }

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


    public async Task<ServiceResult<bool>> GenerateMonthlyInvoicesAsync(string userId)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // Buscar clientes con facturación mensual activa
        var clientsWithMonthly = await _context.Clients
            .Where(c => (c.BillingFrequency == "Monthly" || c.ServiceMode == "Mensual")
                        && c.IsActive)
            .ToListAsync();

        Console.WriteLine($"🔍 Clientes mensuales encontrados: {clientsWithMonthly.Count}");
        foreach (var c in clientsWithMonthly)
            Console.WriteLine($"   - {c.CompanyName} | BillingFrequency: '{c.BillingFrequency}' | MonthlyRate: {c.MonthlyRate} | IsActive: {c.IsActive}");

        if (!clientsWithMonthly.Any())
        {
            return ServiceResult<bool>.Failure("No se encontraron clientes con facturación mensual activa");
        }

        var invoicesCreated = 0;

        foreach (var client in clientsWithMonthly)
        {
            // Verificar si ya existe factura para este mes
            var existingInvoice = await _context.Invoices
                .AnyAsync(i => i.ClientId == client.Id &&
                              i.InvoiceType == "Monthly" &&
                              i.InvoiceDate.Month == currentMonth.Month &&
                              i.InvoiceDate.Year == currentMonth.Year);

            if (existingInvoice)
            {
                continue; // Ya tiene factura este mes
            }

            var invoiceNumber = await GenerateInvoiceNumber();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                ClientId = client.Id,
                InvoiceDate = currentMonth,
                DueDate = currentMonth.AddDays(30),
                InvoiceType = "Monthly",
                Status = "Pendiente",
                CreatedByUserId = userId,
                Notes = $"Factura mensual - {currentMonth:MMMM yyyy}"
            };

            var subtotal = (client.MonthlyRate.HasValue && client.MonthlyRate.Value > 0)
            ? (decimal)client.MonthlyRate.Value
            : 0m;
            invoice.Subtotal = subtotal;
            invoice.Tax = subtotal * 0.16m;
            invoice.Total = invoice.Subtotal + invoice.Tax;

            var item = new InvoiceItem
            {
                Description = $"Póliza mensual de servicios - {currentMonth:MMMM yyyy}",
                Quantity = 1,
                UnitPrice = subtotal,
                Subtotal = subtotal
            };

            invoice.Items.Add(item);

            _context.Invoices.Add(invoice);
            invoicesCreated++;
        }

        await _context.SaveChangesAsync();

        Console.WriteLine($"✅ Facturas mensuales generadas: {invoicesCreated}");

        return ServiceResult<bool>.Success(true);
    }

    public async Task<InvoiceStatsDto> GetInvoiceStatsAsync()
    {
        var invoices = await _context.Invoices.ToListAsync();

        var stats = new InvoiceStatsDto
        {
            PaidInvoices = invoices.Count(i => i.Status == "Pagada"),
            TotalPaid = invoices.Where(i => i.Status == "Pagada").Sum(i => (decimal?)i.Total) ?? 0,
            PendingInvoices = invoices.Count(i => i.Status == "Pendiente"),
            TotalPending = invoices.Where(i => i.Status == "Pendiente").Sum(i => (decimal?)i.Total) ?? 0,
            OverdueInvoices = invoices.Count(i => i.Status == "Vencida"),
            TotalOverdue = invoices.Where(i => i.Status == "Vencida").Sum(i => (decimal?)i.Total) ?? 0,
            TotalInvoices = invoices.Count,
            TotalBilled = invoices.Sum(i => (decimal?)i.Total) ?? 0
        };

        return stats;
    }

    // ⭐ MÉTODO CORREGIDO: GenerateInvoicePdfAsync con tickets
    public async Task<byte[]> GenerateInvoicePdfAsync(int id)
    {
        try
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
            {
                throw new Exception("Factura no encontrada");
            }

            var user = await _userManager.FindByIdAsync(invoice.CreatedByUserId);

            Console.WriteLine("✅ Iniciando generación del PDF de factura...");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, invoice));
                    page.Content().Element(c => ComposeContent(c, invoice));
                    page.Footer().Element(c => ComposeFooter(c, user?.UserName ?? "Sistema"));
                });
            });

            Console.WriteLine("✅ Documento creado, generando PDF bytes...");
            var pdfBytes = document.GeneratePdf();
            Console.WriteLine($"✅ PDF generado exitosamente: {pdfBytes.Length} bytes");

            return pdfBytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR en GenerateInvoicePdfAsync: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private void ComposeHeader(IContainer container, Invoice invoice)
    {
        var purpleColor = new Color(0xFF6B46C1);
        var orangeColor = new Color(0xFFF97316);

        container.Column(column =>
        {
            column.Item().BorderBottom(3).BorderColor(purpleColor).PaddingBottom(10).Row(row =>
            {
                row.ConstantItem(120).Column(logoCol =>
                {
                    var logoPath = Path.Combine(_environment.WebRootPath, "images", "LogoFinesoft.png");
                    if (File.Exists(logoPath))
                    {
                        logoCol.Item().Image(logoPath).FitWidth();
                    }
                    else
                    {
                        logoCol.Item().Text("FINESOFT").FontSize(24).Bold().FontColor(purpleColor);
                    }
                });

                row.RelativeItem().PaddingLeft(20).Column(col =>
                {
                    col.Item().Text("FACTURA").FontSize(28).Bold().FontColor(purpleColor);
                    col.Item().Text(invoice.InvoiceNumber).FontSize(16).FontColor(Colors.Grey.Darken2);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Fecha: {invoice.InvoiceDate:dd/MM/yyyy}").FontSize(10);
                    if (invoice.DueDate.HasValue)
                    {
                        col.Item().Text($"Vencimiento: {invoice.DueDate.Value:dd/MM/yyyy}").FontSize(10);
                    }

                    var statusColor = invoice.Status switch
                    {
                        "Pagada" => Colors.Green.Medium,
                        "Cancelada" => Colors.Red.Medium,
                        "Vencida" => Colors.Red.Darken1,
                        _ => orangeColor
                    };
                    col.Item().Text($"Estado: {invoice.Status}").FontSize(10).Bold().FontColor(statusColor);
                });
            });

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Background(purpleColor).Padding(5)
                        .Text("INFORMACIÓN DEL CLIENTE").FontSize(11).Bold().FontColor(Colors.White);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Text($"Empresa: {invoice.Client?.CompanyName}").FontSize(10);
                    col.Item().Text($"RFC: {invoice.Client?.RFC}").FontSize(10);
                    col.Item().Text($"Email: {invoice.Client?.Email}").FontSize(10);
                    col.Item().Text($"Teléfono: {invoice.Client?.Phone}").FontSize(10);
                });

                row.ConstantItem(20);

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Background(orangeColor).Padding(5)
                        .Text("DATOS DE FACTURACIÓN").FontSize(11).Bold().FontColor(Colors.White);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Text($"Tipo: {invoice.InvoiceType}").FontSize(10);
                    col.Item().Text($"Dirección: {invoice.Client?.Address}").FontSize(10);
                    if (!string.IsNullOrEmpty(invoice.PaymentMethod))
                    {
                        col.Item().Text($"Método de pago: {invoice.PaymentMethod}").FontSize(10);
                    }
                });
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        var purpleColor = "#6B46C1";
        var orangeColor = "#F97316";
        var infoColor = "#0EA5E9";

        container.PaddingVertical(20).Column(column =>
        {
            // ⭐ NUEVA SECCIÓN: Tickets Asociados (igual que en cotizaciones)
            var ticketItems = invoice.Items?
                .Where(i => i.TicketId.HasValue && i.Ticket != null)
                .ToList() ?? new List<InvoiceItem>();

            if (ticketItems.Any())
            {
                column.Item().Text("TICKETS ASOCIADOS").FontSize(14).Bold().FontColor(infoColor);
                column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(infoColor);

                foreach (var item in ticketItems)
                {
                    try
                    {
                        var ticket = item.Ticket!;
                        var clientName = ticket.Project?.Client?.CompanyName ?? "N/A";
                        var projectName = ticket.Project?.Name ?? "N/A";
                        var hours = ticket.ActualHours;
                        var hourlyRate = ticket.Project?.HourlyRate ?? 0;
                        var description = ticket.Description ?? string.Empty;
                        var ticketTitle = ticket.Title ?? "Sin título";

                        column.Item().PaddingVertical(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Blue.Lighten5).Padding(15).Column(ticketCol =>
                            {
                                // Título del ticket
                                ticketCol.Item().Row(row =>
                                {
                                    row.ConstantItem(80).Text($"Ticket #{ticket.Id}").FontSize(12).Bold()
                                        .FontColor(infoColor);
                                    row.RelativeItem().Text(ticketTitle).FontSize(12).Bold();
                                });

                                ticketCol.Item().PaddingVertical(8).LineHorizontal(1)
                                    .LineColor(Colors.Grey.Lighten2);

                                // Información en 2 columnas
                                ticketCol.Item().Row(row =>
                                {
                                    // Columna izquierda
                                    row.RelativeItem().Column(leftCol =>
                                    {
                                        leftCol.Item().PaddingBottom(8).Row(infoRow =>
                                        {
                                            infoRow.ConstantItem(70).AlignLeft().Text("Cliente:").FontSize(10)
                                                .Bold();
                                            infoRow.RelativeItem().AlignLeft().Text(clientName).FontSize(10);
                                        });

                                        leftCol.Item().PaddingBottom(8).Row(infoRow =>
                                        {
                                            infoRow.ConstantItem(70).AlignLeft().Text("Proyecto:").FontSize(10)
                                                .Bold();
                                            infoRow.RelativeItem().AlignLeft().Text(projectName).FontSize(10);
                                        });
                                    });

                                    row.ConstantItem(20);

                                    // Columna derecha
                                    row.RelativeItem().Column(rightCol =>
                                    {
                                        rightCol.Item().PaddingBottom(8).Row(infoRow =>
                                        {
                                            infoRow.ConstantItem(70).AlignLeft().Text("Horas:").FontSize(10).Bold();
                                            infoRow.RelativeItem().AlignLeft().Text($"{hours:0.0} h").FontSize(10);
                                        });

                                        if (hourlyRate > 0)
                                        {
                                            var ticketCost = hours * hourlyRate;
                                            rightCol.Item().PaddingBottom(8).Row(infoRow =>
                                            {
                                                infoRow.ConstantItem(70).AlignLeft().Text("Costo:").FontSize(10)
                                                    .Bold();
                                                infoRow.RelativeItem().AlignLeft().Text($"${ticketCost:N2}")
                                                    .FontSize(10).FontColor(Colors.Green.Medium);
                                            });
                                        }
                                    });
                                });

                                // Descripción
                                if (!string.IsNullOrWhiteSpace(description))
                                {
                                    ticketCol.Item().PaddingTop(8).LineHorizontal(1)
                                        .LineColor(Colors.Grey.Lighten2);
                                    ticketCol.Item().PaddingTop(8).Column(descCol =>
                                    {
                                        descCol.Item().Text("Descripción:").FontSize(10).Bold();
                                        descCol.Item().PaddingTop(4).Text(description).FontSize(9)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                                }
                            });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error renderizando ticket {item.TicketId}: {ex.Message}");
                    }
                }

                column.Item().PaddingTop(20);
            }

            // ⭐ TABLA DE ARTÍCULOS
            column.Item().Text("ARTÍCULOS").FontSize(14).Bold().FontColor(purpleColor);
            column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(purpleColor);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(purpleColor).Padding(8).Text("Descripción")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignCenter().Text("Ticket")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignCenter().Text("Cantidad")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignRight().Text("Precio Unit.")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignRight().Text("Subtotal")
                        .FontColor(Colors.White).Bold();
                });

                var isAlternate = false;
                foreach (var item in invoice.Items ?? new List<InvoiceItem>())
                {
                    var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                    var description = item.Description ?? "Sin descripción";
                    var ticketText = item.TicketId.HasValue ? $"#{item.TicketId}" : "-";

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text(description);

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(ticketText).FontSize(9).FontColor(infoColor);

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(item.Quantity.ToString("0"));

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"${item.UnitPrice:N2}");

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"${item.Subtotal:N2}");

                    isAlternate = !isAlternate;
                }
            });

            // ⭐ TOTALES
            column.Item().PaddingTop(20).AlignRight().Column(totalsColumn =>
            {
                totalsColumn.Item().Border(2).BorderColor(purpleColor).Padding(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal:").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text($"${invoice.Subtotal:N2}").FontSize(11);
                    });

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("IVA (16%):").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text($"${invoice.Tax:N2}").FontSize(11);
                    });

                    col.Item().PaddingTop(10).BorderTop(2).BorderColor(orangeColor).PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL:").FontSize(16).Bold().FontColor(purpleColor);
                        row.RelativeItem().AlignRight().Text($"${invoice.Total:N2}").FontSize(16).Bold()
                            .FontColor(orangeColor);
                    });

                    if (invoice.Payments.Any())
                    {
                        var totalPaid = invoice.Payments.Sum(p => p.Amount);
                        var balance = invoice.Total - totalPaid;

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("Pagado:").FontSize(11).FontColor(Colors.Green.Medium);
                            row.RelativeItem().AlignRight().Text($"${totalPaid:N2}").FontSize(11)
                                .FontColor(Colors.Green.Medium);
                        });

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Saldo:").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text($"${balance:N2}").Bold().FontSize(11);
                        });
                    }
                });
            });

            if (invoice.Payments.Any())
            {
                column.Item().PaddingTop(20).Column(paymentsCol =>
                {
                    paymentsCol.Item().Text("Historial de Pagos").FontSize(14).Bold().FontColor(purpleColor);
                    paymentsCol.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(purpleColor).Padding(5).Text("Fecha")
                                .FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(purpleColor).Padding(5).Text("Método")
                                .FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(purpleColor).Padding(5).AlignRight().Text("Monto")
                                .FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(purpleColor).Padding(5).Text("Referencia")
                                .FontColor(Colors.White).FontSize(9).Bold();
                        });

                        foreach (var payment in invoice.Payments)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text($"{payment.PaymentDate:dd/MM/yyyy}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(payment.PaymentMethod).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                .Text($"${payment.Amount:N2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(payment.Reference).FontSize(8);
                        }
                    });
                });
            }

            if (!string.IsNullOrEmpty(invoice.Notes))
            {
                column.Item().PaddingTop(20).Border(1).BorderColor(orangeColor).Background("#FFF7ED")
                    .Padding(15).Column(col =>
                    {
                        col.Item().Text("Notas:").Bold().FontColor(orangeColor).FontSize(12);
                        col.Item().PaddingTop(5).Text(invoice.Notes).FontSize(10);
                    });
            }
        });
    }

    private void ComposeFooter(IContainer container, string createdBy)
    {
        var purpleColor = "#6B46C1";

        container.BorderTop(2).BorderColor(purpleColor).PaddingTop(10).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("FINESOFT").FontSize(10).Bold().FontColor(purpleColor);
                    col.Item().Text("Blvd. Juan de Dios Batiz #145 PTE").FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().Text("Teléfono: (668) 817-0400").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignCenter().Column(col =>
                {
                    col.Item().Text($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("www.finesoft.com.mx").FontSize(8).FontColor(purpleColor).Underline();
                    col.Item().Text("informes@finesoft.com.mx").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });
    }

    public async Task<List<int>> GetTicketsInUseAsync()
    {
        return await _context.InvoiceItems
            .Where(ii => ii.TicketId.HasValue)
            .Select(ii => ii.TicketId!.Value)
            .Distinct()
            .ToListAsync();
    }

    private async Task<string> GenerateInvoiceNumber()
    {
        var year = DateTime.UtcNow.Year;
        var lastInvoice = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}"))
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"INV-{year}-{nextNumber:D4}";
    }

    // ⭐ MÉTODO CORREGIDO: MapToDetailDto con información completa de tickets
    private async Task<InvoiceDetailDto> MapToDetailDto(Invoice invoice)
    {
        var user = await _userManager.FindByIdAsync(invoice.CreatedByUserId);

        var totalPaid = invoice.Payments?.Sum(p => p.Amount) ?? 0m;

        // Reglas de negocio:
        // - Pagada: saldo 0
        // - Cancelada: saldo 0 (irrelevante cobrar)
        // - Pendiente: total - pagado
        var balance = invoice.Status switch
        {
            "Pagada" => 0m,
            "Cancelada" => 0m,
            _ => invoice.Total - totalPaid
        };

        var dto = new InvoiceDetailDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,

            ClientId = invoice.ClientId,
            ClientName = invoice.Client?.CompanyName ?? string.Empty,
            ClientEmail = invoice.Client?.Email ?? string.Empty,
            ClientRFC = invoice.Client?.RFC ?? string.Empty,

            QuoteId = invoice.QuoteId,
            QuoteNumber = invoice.Quote?.QuoteNumber,

            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,

            InvoiceType = invoice.InvoiceType,
            Status = invoice.Status,
            PaymentMethod = invoice.PaymentMethod,
            PaymentType = invoice.PaymentType,

            CreatedByUserId = invoice.CreatedByUserId,
            CreatedByUserName = user?.UserName ?? string.Empty,

            Subtotal = invoice.Subtotal,
            Tax = invoice.Tax,
            Total = invoice.Total,
            TicketCount = invoice.Items?.Count(i => i.TicketId.HasValue && i.TicketId > 0) ?? 0,

            // Si está pagada, muestra el total como pagado; si no, lo acumulado.
            PaidAmount = invoice.Status == "Pagada" ? invoice.Total : totalPaid,

            Balance = balance,
            PaidDate = invoice.PaidDate,

            Notes = invoice.Notes,

            // NUEVO: cancelación
            CancellationReason = invoice.CancellationReason,
            CancelledDate = invoice.CancelledDate
        };

        if (invoice.Items != null && invoice.Items.Any())
        {
            dto.Items = invoice.Items.Select(i => new InvoiceItemDetailDto
            {
                Id = i.Id,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ServiceId = null,
                ServiceName = null,

                TicketId = i.TicketId,
                TicketTitle = i.Ticket?.Title,
                TicketDescription = i.Ticket?.Description,
                TicketClientName = i.Ticket?.Project?.Client?.CompanyName,
                TicketProjectName = i.Ticket?.Project?.Name,
                TicketActualHours = i.Ticket?.ActualHours,
                TicketHourlyRate = i.Ticket?.Project?.HourlyRate
            }).ToList();
        }

        if (invoice.Payments != null && invoice.Payments.Any())
        {
            foreach (var payment in invoice.Payments)
            {
                var paymentUser = await _userManager.FindByIdAsync(payment.RecordedByUserId);

                dto.Payments.Add(new InvoicePaymentDto
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    Reference = payment.Reference,
                    Notes = payment.Notes,
                    RecordedByUserId = payment.RecordedByUserId,
                    RecordedByUserName = paymentUser?.UserName,

                    // ✅ AGREGAR ESTO
                    ReceiptPath = payment.ReceiptPath,
                    ReceiptFileName = payment.ReceiptFileName,
                    ReceiptContentType = payment.ReceiptContentType,
                    ReceiptSize = payment.ReceiptSize,
                    ReceiptUploadedAt = payment.ReceiptUploadedAt
                });

            }
        }

        return dto;
    }
}
