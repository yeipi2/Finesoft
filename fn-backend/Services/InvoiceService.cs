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

namespace fs_backend.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public InvoiceService(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IEnumerable<InvoiceDetailDto>> GetInvoicesAsync(string? status = null, string? invoiceType = null, int? clientId = null)
    {
        var query = _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .Include(i => i.Items)
            // CÓDIGO FUTURO - Include de Service deshabilitado
            // .ThenInclude(item => item.Service)
            .Include(i => i.Items)
            .ThenInclude(item => item.Ticket)
            .Include(i => i.Payments)
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
            .Include(i => i.Quote)
            .Include(i => i.Items)
            // CÓDIGO FUTURO - Include de Service deshabilitado
            // .ThenInclude(item => item.Service)
            .Include(i => i.Items)
            .ThenInclude(item => item.Ticket)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice == null ? null : await MapToDetailDto(invoice);
    }

    public async Task<ServiceResult<InvoiceDetailDto>> CreateInvoiceAsync(InvoiceDto invoiceDto, string createdByUserId)
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
            PaymentMethod = invoiceDto.PaymentMethod,
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
                // CÓDIGO FUTURO - ServiceId deshabilitado
                // ServiceId = itemDto.ServiceId,
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

        return ServiceResult<InvoiceDetailDto>.Success(await MapToDetailDto(invoice));
    }

    public async Task<ServiceResult<InvoiceDetailDto>> CreateInvoiceFromQuoteAsync(CreateInvoiceFromQuoteDto dto, string createdByUserId)
    {
        var quote = await _context.Quotes
            .Include(q => q.Client)
            .Include(q => q.Items)
            // CÓDIGO FUTURO - Include de Service deshabilitado
            // .ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(q => q.Id == dto.QuoteId);

        if (quote == null)
        {
            return ServiceResult<InvoiceDetailDto>.Failure("Cotización no encontrada");
        }

        if (quote.Status != "Aceptada")
        {
            return ServiceResult<InvoiceDetailDto>.Failure("Solo se pueden facturar cotizaciones aceptadas");
        }

        var existingInvoice = await _context.Invoices.AnyAsync(i => i.QuoteId == dto.QuoteId);
        if (existingInvoice)
        {
            return ServiceResult<InvoiceDetailDto>.Failure("Esta cotización ya tiene una factura asociada");
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
            Status = "Pending",
            PaymentMethod = dto.PaymentMethod,
            CreatedByUserId = createdByUserId,
            Notes = dto.Notes,
            Subtotal = quote.Subtotal,
            Tax = quote.Tax,
            Total = quote.Total
        };

        foreach (var quoteItem in quote.Items)
        {
            var invoiceItem = new InvoiceItem
            {
                Description = quoteItem.Description,
                Quantity = quoteItem.Quantity,
                UnitPrice = quoteItem.UnitPrice,
                Subtotal = quoteItem.Subtotal
                // CÓDIGO FUTURO - ServiceId deshabilitado
                // ServiceId = quoteItem.ServiceId
            };

            invoice.Items.Add(invoiceItem);
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        await _context.Entry(invoice).Reference(i => i.Client).LoadAsync();
        await _context.Entry(invoice).Collection(i => i.Items).LoadAsync();

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

        if (invoice.Status == "Paid")
        {
            return ServiceResult<InvoiceDetailDto>.Failure("No se puede modificar una factura pagada");
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

        invoice.ClientId = invoiceDto.ClientId;
        invoice.InvoiceDate = invoiceDto.InvoiceDate ?? invoice.InvoiceDate;
        invoice.DueDate = invoiceDto.DueDate;
        invoice.InvoiceType = invoiceDto.InvoiceType;
        invoice.Status = invoiceDto.Status;
        invoice.PaymentMethod = invoiceDto.PaymentMethod;
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
                // CÓDIGO FUTURO - ServiceId deshabilitado
                // ServiceId = itemDto.ServiceId,
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

        return ServiceResult<InvoiceDetailDto>.Success(await MapToDetailDto(invoice));
    }

    public async Task<ServiceResult<bool>> DeleteInvoiceAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return ServiceResult<bool>.Failure("Factura no encontrada");
        }

        if (invoice.Payments.Any())
        {
            return ServiceResult<bool>.Failure("No se puede eliminar una factura con pagos registrados");
        }

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ChangeInvoiceStatusAsync(int id, string newStatus)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
        {
            return ServiceResult<bool>.Failure("Factura no encontrada");
        }

        var validStatuses = new[] { "Pending", "Paid", "Overdue", "Cancelled" };
        if (!validStatuses.Contains(newStatus))
        {
            return ServiceResult<bool>.Failure("Estado no válido");
        }

        invoice.Status = newStatus;

        if (newStatus == "Paid" && !invoice.PaidDate.HasValue)
        {
            invoice.PaidDate = DateTime.UtcNow;
        }

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<InvoicePaymentDto>> AddPaymentAsync(int invoiceId, InvoicePaymentDto paymentDto, string userId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            return ServiceResult<InvoicePaymentDto>.Failure("Factura no encontrada");
        }

        var totalPaid = invoice.Payments.Sum(p => p.Amount);
        var balance = invoice.Total - totalPaid;

        if (paymentDto.Amount > balance)
        {
            return ServiceResult<InvoicePaymentDto>.Failure($"El monto del pago excede el saldo pendiente de ${balance:N2}");
        }

        var payment = new InvoicePayment
        {
            InvoiceId = invoiceId,
            Amount = paymentDto.Amount,
            PaymentDate = paymentDto.PaymentDate,
            PaymentMethod = paymentDto.PaymentMethod,
            Reference = paymentDto.Reference,
            Notes = paymentDto.Notes,
            RecordedByUserId = userId
        };

        _context.Set<InvoicePayment>().Add(payment);

        totalPaid += paymentDto.Amount;

        if (totalPaid >= invoice.Total)
        {
            invoice.Status = "Paid";
            invoice.PaidDate = DateTime.UtcNow;
        }

        _context.Invoices.Update(invoice);
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
            RecordedByUserName = user?.UserName
        });
    }

    public async Task<ServiceResult<bool>> GenerateMonthlyInvoicesAsync(string userId)
    {
        var monthlyClients = await _context.Clients
            .Where(c => c.ServiceMode == "Mensualidad" && c.IsActive && c.MonthlyRate.HasValue)
            .ToListAsync();

        if (!monthlyClients.Any())
        {
            return ServiceResult<bool>.Failure("No hay clientes con modalidad de mensualidad activos");
        }

        var currentMonth = DateTime.UtcNow;
        var invoicesCreated = 0;

        foreach (var client in monthlyClients)
        {
            var existingInvoice = await _context.Invoices
                .AnyAsync(i => i.ClientId == client.Id &&
                              i.InvoiceType == "Monthly" &&
                              i.InvoiceDate.Month == currentMonth.Month &&
                              i.InvoiceDate.Year == currentMonth.Year);

            if (existingInvoice)
                continue;

            var invoiceNumber = await GenerateInvoiceNumber();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                ClientId = client.Id,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                InvoiceType = "Monthly",
                Status = "Pending",
                CreatedByUserId = userId,
                Notes = $"Factura mensual - {currentMonth:MMMM yyyy}"
            };

            var subtotal = (decimal)client.MonthlyRate!.Value;
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

    public async Task<byte[]> GenerateInvoicePdfAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Items)
            // CÓDIGO FUTURO - Include de Service deshabilitado
            // .ThenInclude(item => item.Service)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            throw new Exception("Factura no encontrada");
        }

        var user = await _userManager.FindByIdAsync(invoice.CreatedByUserId);

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

        return document.GeneratePdf();
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
                        "Paid" => Colors.Green.Medium,
                        "Cancelled" => Colors.Red.Medium,
                        "Overdue" => Colors.Red.Darken1,
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

        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(purpleColor).Padding(8).Text("Descripción").FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignCenter().Text("Cantidad").FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignRight().Text("Precio Unit.").FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignRight().Text("Subtotal").FontColor(Colors.White).Bold();
                });

                var isAlternate = false;
                foreach (var item in invoice.Items)
                {
                    var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text(item.Description);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignCenter().Text(item.Quantity.ToString());
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"${item.UnitPrice:N2}");
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).AlignRight().Text($"${item.Subtotal:N2}");

                    isAlternate = !isAlternate;
                }
            });

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
                        row.RelativeItem().AlignRight().Text($"${invoice.Total:N2}").FontSize(16).Bold().FontColor(orangeColor);
                    });

                    if (invoice.Payments.Any())
                    {
                        var totalPaid = invoice.Payments.Sum(p => p.Amount);
                        var balance = invoice.Total - totalPaid;

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("Pagado:").FontSize(11).FontColor(Colors.Green.Medium);
                            row.RelativeItem().AlignRight().Text($"${totalPaid:N2}").FontSize(11).FontColor(Colors.Green.Medium);
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
                            header.Cell().Background(purpleColor).Padding(5).Text("Fecha").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(purpleColor).Padding(5).Text("Método").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(purpleColor).Padding(5).AlignRight().Text("Monto").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(purpleColor).Padding(5).Text("Referencia").FontColor(Colors.White).FontSize(9).Bold();
                        });

                        foreach (var payment in invoice.Payments)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{payment.PaymentDate:dd/MM/yyyy}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(payment.PaymentMethod).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${payment.Amount:N2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(payment.Reference).FontSize(8);
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
                    col.Item().Text($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("www.finesoft.com.mx").FontSize(8).FontColor(purpleColor).Underline();
                    col.Item().Text("informes@finesoft.com.mx").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });
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

    private async Task<InvoiceDetailDto> MapToDetailDto(Invoice invoice)
    {
        var user = await _userManager.FindByIdAsync(invoice.CreatedByUserId);
        var totalPaid = invoice.Payments?.Sum(p => p.Amount) ?? 0;

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
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedByUserName = user?.UserName ?? string.Empty,
            Subtotal = invoice.Subtotal,
            Tax = invoice.Tax,
            Total = invoice.Total,
            PaidAmount = totalPaid,
            Balance = invoice.Total - totalPaid,
            PaidDate = invoice.PaidDate,
            Notes = invoice.Notes
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
                // CÓDIGO FUTURO - ServiceId y ServiceName deshabilitados
                ServiceId = null,
                ServiceName = null,
                // ServiceId = i.ServiceId,
                // ServiceName = i.Service?.Name,
                TicketId = i.TicketId,
                TicketTitle = i.Ticket?.Title
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
                    RecordedByUserName = paymentUser?.UserName
                });
            }
        }

        return dto;
    }
}