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

public class QuoteService : IQuoteService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailService _emailService;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IWebHostEnvironment environment,
        IEmailService emailService,
        ILogger<QuoteService> logger)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<QuoteDetailDto>> GetQuotesAsync(string? status = null, int? clientId = null)
    {
        var query = _context.Quotes
            .Include(q => q.Client)
            .Include(q => q.Items)
            .ThenInclude(i => i.Ticket)
                .ThenInclude(t => t.Project)
                    .ThenInclude(p => p.Client)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(q => q.Status == status);
        }

        if (clientId.HasValue)
        {
            query = query.Where(q => q.ClientId == clientId.Value);
        }

        var quotes = await query.OrderByDescending(q => q.CreatedAt).ToListAsync();

        var quoteDtos = new List<QuoteDetailDto>();
        foreach (var quote in quotes)
        {
            quoteDtos.Add(await MapToDetailDto(quote));
        }

        return quoteDtos;
    }

    public async Task<QuoteDetailDto?> GetQuoteByIdAsync(int id)
    {
        var quote = await _context.Quotes
            .Include(q => q.Client)
            .Include(q => q.Items)
            .ThenInclude(i => i.Ticket)
                .ThenInclude(t => t.Project)
                    .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quote == null ? null : await MapToDetailDto(quote);
    }

    public async Task<ServiceResult<QuoteDetailDto>> CreateQuoteAsync(QuoteDto quoteDto, string createdByUserId)
    {
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == quoteDto.ClientId);
        if (!clientExists)
        {
            return ServiceResult<QuoteDetailDto>.Failure("El cliente especificado no existe");
        }

        if (quoteDto.Items == null || !quoteDto.Items.Any())
        {
            return ServiceResult<QuoteDetailDto>.Failure("La cotización debe tener al menos un elemento");
        }

        var quoteNumber = await GenerateQuoteNumber();

        var quote = new Quote
        {
            QuoteNumber = quoteNumber,
            ClientId = quoteDto.ClientId,
            CreatedAt = DateTime.UtcNow,
            ValidUntil = quoteDto.ValidUntil,
            Status = quoteDto.Status,
            CreatedByUserId = createdByUserId,
            Notes = quoteDto.Notes
        };

        decimal subtotal = 0;
        foreach (var itemDto in quoteDto.Items)
        {
            var itemSubtotal = itemDto.Quantity * itemDto.UnitPrice;
            subtotal += itemSubtotal;

            var item = new QuoteItem
            {
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Subtotal = itemSubtotal,
                TicketId = itemDto.TicketId
            };

            quote.Items.Add(item);
        }

        quote.Subtotal = subtotal;
        quote.Tax = subtotal * 0.16m;
        quote.Total = quote.Subtotal + quote.Tax;

        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();

        await _context.Entry(quote).Reference(q => q.Client).LoadAsync();
        await _context.Entry(quote).Collection(q => q.Items).LoadAsync();

        foreach (var item in quote.Items)
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

        return ServiceResult<QuoteDetailDto>.Success(await MapToDetailDto(quote));
    }

    public async Task<ServiceResult<QuoteDetailDto>> UpdateQuoteAsync(int id, QuoteDto quoteDto)
    {
        var quote = await _context.Quotes
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quote == null)
        {
            return ServiceResult<QuoteDetailDto>.Failure("Cotización no encontrada");
        }

        var clientExists = await _context.Clients.AnyAsync(c => c.Id == quoteDto.ClientId);
        if (!clientExists)
        {
            return ServiceResult<QuoteDetailDto>.Failure("El cliente especificado no existe");
        }

        if (quoteDto.Items == null || !quoteDto.Items.Any())
        {
            return ServiceResult<QuoteDetailDto>.Failure("La cotización debe tener al menos un elemento");
        }

        quote.ClientId = quoteDto.ClientId;
        quote.ValidUntil = quoteDto.ValidUntil;
        quote.Status = quoteDto.Status;
        quote.Notes = quoteDto.Notes;
        quote.UpdatedAt = DateTime.UtcNow;

        _context.Set<QuoteItem>().RemoveRange(quote.Items);

        decimal subtotal = 0;
        quote.Items.Clear();

        foreach (var itemDto in quoteDto.Items)
        {
            var itemSubtotal = itemDto.Quantity * itemDto.UnitPrice;
            subtotal += itemSubtotal;

            var item = new QuoteItem
            {
                QuoteId = id,
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Subtotal = itemSubtotal,
                TicketId = itemDto.TicketId
            };

            quote.Items.Add(item);
        }

        quote.Subtotal = subtotal;
        quote.Tax = subtotal * 0.16m;
        quote.Total = quote.Subtotal + quote.Tax;

        _context.Quotes.Update(quote);
        await _context.SaveChangesAsync();

        await _context.Entry(quote).Reference(q => q.Client).LoadAsync();
        await _context.Entry(quote).Collection(q => q.Items).LoadAsync();

        foreach (var item in quote.Items)
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

        return ServiceResult<QuoteDetailDto>.Success(await MapToDetailDto(quote));
    }

    public async Task<ServiceResult<bool>> DeleteQuoteAsync(int id)
    {
        var quote = await _context.Quotes.FindAsync(id);
        if (quote == null)
        {
            return ServiceResult<bool>.Failure("Cotización no encontrada");
        }

        _context.Quotes.Remove(quote);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ChangeQuoteStatusAsync(int id, string newStatus)
    {
        var quote = await _context.Quotes.FindAsync(id);
        if (quote == null)
        {
            return ServiceResult<bool>.Failure("Cotización no encontrada");
        }

        var validStatuses = new[] { "Borrador", "Enviada", "Aceptada", "Rechazada", "Expirada" };
        if (!validStatuses.Contains(newStatus))
        {
            return ServiceResult<bool>.Failure("Estado no válido");
        }

        quote.Status = newStatus;
        quote.UpdatedAt = DateTime.UtcNow;
        _context.Quotes.Update(quote);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<byte[]> GenerateQuotePdfAsync(int id)
    {
        try
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.Items)
                    .ThenInclude(i => i.Ticket)
                        .ThenInclude(t => t!.Project)
                            .ThenInclude(p => p!.Client)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                throw new Exception("Cotización no encontrada");
            }

            var user = await _userManager.FindByIdAsync(quote.CreatedByUserId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, quote));
                    page.Content().Element(c => ComposeContent(c, quote));
                    page.Footer().Element(c => ComposeFooter(c, user?.UserName ?? "Sistema"));
                });
            });

            var pdfBytes = document.GeneratePdf();
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando PDF para cotización {QuoteId}", id);
            throw;
        }
    }

    public async Task<QuoteDetailDto?> GetQuoteByPublicTokenAsync(string token)
    {
        var quote = await _context.Quotes
            .Include(q => q.Client)
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.PublicToken == token);

        if (quote == null) return null;

        return await MapToDetailDto(quote);
    }

    public async Task<ServiceResult<bool>> RespondToQuoteAsync(string token, string status, string? comments)
    {
        var quote = await _context.Quotes
            .FirstOrDefaultAsync(q => q.PublicToken == token);

        if (quote == null)
        {
            return ServiceResult<bool>.Failure("Cotización no encontrada");
        }

        if (quote.Status == "Aceptada" || quote.Status == "Rechazada")
        {
            return ServiceResult<bool>.Failure("Esta cotización ya fue respondida");
        }

        quote.Status = status;
        quote.Notes = string.IsNullOrEmpty(comments)
            ? quote.Notes
            : $"{quote.Notes}\n\n[Respuesta del cliente]: {comments}";
        quote.UpdatedAt = DateTime.UtcNow;

        _context.Quotes.Update(quote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Cotización {QuoteId} respondida como '{Status}' por el cliente",
            quote.Id, status);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> SendQuoteEmailAsync(int quoteId)
    {
        try
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null)
            {
                return ServiceResult<bool>.Failure("Cotización no encontrada");
            }

            // Generar token único si no existe
            if (string.IsNullOrEmpty(quote.PublicToken))
            {
                quote.PublicToken = Guid.NewGuid().ToString("N");
                _context.Quotes.Update(quote);
                await _context.SaveChangesAsync();
            }

            var pdfBytes = await GenerateQuotePdfAsync(quoteId);

            var emailSent = await _emailService.SendQuoteEmailAsync(
                quote.Client.Email,
                quote.Client.CompanyName,
                quote.QuoteNumber,
                pdfBytes,
                quote.PublicToken
            );

            if (emailSent)
            {
                quote.Status = "Enviada";
                quote.UpdatedAt = DateTime.UtcNow;
                _context.Quotes.Update(quote);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Cotización {QuoteId} enviada por email", quoteId);
                return ServiceResult<bool>.Success(true);
            }

            return ServiceResult<bool>.Failure("Error al enviar el email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar cotización {QuoteId}", quoteId);
            return ServiceResult<bool>.Failure($"Error: {ex.Message}");
        }
    }

    private void ComposeHeader(IContainer container, Quote quote)
    {
        var purpleColor = new Color(0xFF6B46C1);

        container.BorderBottom(3).BorderColor(purpleColor).PaddingBottom(10).Row(row =>
        {
            row.ConstantItem(120).Column(logoCol =>
            {
                var logoPath = Path.Combine(_environment.WebRootPath, "images", "LogoFinesoft.png");
                if (File.Exists(logoPath))
                    logoCol.Item().Image(logoPath).FitWidth();
                else
                    logoCol.Item().Text("FINESOFT").FontSize(24).Bold().FontColor(purpleColor);
            });

            row.RelativeItem().PaddingLeft(20).Column(col =>
            {
                col.Item().Text("COTIZACIÓN").FontSize(28).Bold().FontColor(purpleColor);
                col.Item().Text(quote.QuoteNumber).FontSize(16).FontColor(Colors.Grey.Darken2);
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"Fecha: {quote.CreatedAt:dd/MM/yyyy}").FontSize(10);
                if (quote.ValidUntil.HasValue)
                    col.Item().Text($"Válida hasta: {quote.ValidUntil.Value:dd/MM/yyyy}").FontSize(10);

                var statusColor = quote.Status switch
                {
                    "Aceptada" => Colors.Green.Medium,
                    "Rechazada" => Colors.Red.Medium,
                    "Expirada" => Colors.Red.Darken1,
                    "Enviada" => Colors.Grey.Medium,
                    _ => Colors.Grey.Medium
                };

                col.Item().Row(r =>
                {
                    r.AutoItem().Text("Estado: ").FontSize(10).Bold().FontColor(Colors.Black);
                    r.AutoItem().Text(quote.Status).FontSize(10).Bold().FontColor(statusColor);
                });
            });
        });
    }

    private void ComposeContent(IContainer container, Quote quote)
    {
        var purpleColor = "#6B46C1";
        var infoColor = "#6B46C1";

        container.PaddingVertical(20).Column(column =>
        {
            // ── Datos cliente — solo aparece UNA vez (está en Content) ─────────────
            column.Item().PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Background(purpleColor).Padding(5)
                        .Text("INFORMACIÓN DEL CLIENTE").FontSize(11).Bold().FontColor(Colors.White);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Text($"Empresa: {quote.Client?.CompanyName}").FontSize(10);
                    col.Item().Text($"Contacto: {quote.Client?.ContactName}").FontSize(10);
                    col.Item().Text($"Email: {quote.Client?.Email}").FontSize(10);
                    col.Item().Text($"Teléfono: {quote.Client?.Phone}").FontSize(10);
                });

                row.ConstantItem(20);

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Background(purpleColor).Padding(5)
                        .Text("DETALLES DE LA COTIZACIÓN").FontSize(11).Bold().FontColor(Colors.White);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Text($"RFC: {quote.Client?.RFC}").FontSize(10);
                    col.Item().Text($"Dirección: {quote.Client?.Address}").FontSize(10);
                });
            });

            // ── Tickets asociados ──────────────────────────────────────────────────
            var ticketItems = quote.Items?
                .Where(i => i.TicketId.HasValue && i.Ticket != null)
                .ToList() ?? new List<QuoteItem>();

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

                        column.Item().PaddingVertical(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten4).Padding(15).Column(ticketCol =>
                            {
                                ticketCol.Item().Row(row =>
                                {
                                    row.ConstantItem(80).Text($"Ticket #{ticket.Id}").FontSize(12).Bold()
                                        .FontColor(infoColor);
                                    row.RelativeItem().Text(ticket.Title ?? "Sin título").FontSize(12).Bold();
                                });

                                ticketCol.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                ticketCol.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(leftCol =>
                                    {
                                        leftCol.Item().PaddingBottom(8).Row(infoRow =>
                                        {
                                            infoRow.ConstantItem(70).AlignLeft().Text("Cliente:").FontSize(10).Bold();
                                            infoRow.RelativeItem().AlignLeft().Text(clientName).FontSize(10);
                                        });
                                        leftCol.Item().PaddingBottom(8).Row(infoRow =>
                                        {
                                            infoRow.ConstantItem(70).AlignLeft().Text("Proyecto:").FontSize(10).Bold();
                                            infoRow.RelativeItem().AlignLeft().Text(projectName).FontSize(10);
                                        });
                                    });

                                    row.ConstantItem(20);

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
                                                infoRow.ConstantItem(70).AlignLeft().Text("Costo:").FontSize(10).Bold();
                                                infoRow.RelativeItem().AlignLeft()
                                                    .Text($"${ticketCost:N2}").FontSize(10)
                                                    .FontColor(Colors.Green.Medium);
                                            });
                                        }
                                    });
                                });
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error renderizando ticket {TicketId}", item.TicketId);
                    }
                }

                column.Item().PaddingTop(20);
            }

            // ── Artículos ──────────────────────────────────────────────────────────
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
                foreach (var item in quote.Items ?? new List<QuoteItem>())
                {
                    var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                    var ticketText = item.TicketId.HasValue ? $"#{item.TicketId}" : "-";

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Text(item.Description ?? "Sin descripción");
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

            // ── Totales ────────────────────────────────────────────────────────────
            column.Item().PaddingTop(20).AlignRight().Column(totalsColumn =>
            {
                totalsColumn.Item().Border(2).BorderColor(purpleColor).Padding(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal:").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text($"${quote.Subtotal:N2}").FontSize(11);
                    });

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("IVA (16%):").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text($"${quote.Tax:N2}").FontSize(11);
                    });

                    col.Item().PaddingTop(10).BorderTop(2).BorderColor(Colors.Black).PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL:").FontSize(16).Bold().FontColor(Colors.Black);
                        row.RelativeItem().AlignRight().Text($"${quote.Total:N2}").FontSize(16).Bold()
                            .FontColor(Colors.Black);
                    });
                });
            });

            // ── Notas ──────────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(quote.Notes))
            {
                column.Item().PaddingTop(20).Border(1).BorderColor(purpleColor).Background("#F5F0FF")
                    .Padding(15).Column(col =>
                    {
                        col.Item().Text("Notas Adicionales:").Bold().FontColor(purpleColor).FontSize(12);
                        col.Item().PaddingTop(5).Text(quote.Notes).FontSize(10);
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
                    col.Item().Text("Teléfono: (668) 817-1400").FontSize(8).FontColor(Colors.Grey.Medium);
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

            column.Item().PaddingTop(10).AlignCenter()
                .Text("Este documento es una cotización y no representa un compromiso de compra hasta su confirmación formal.")
                .FontSize(7).FontColor(Colors.Grey.Medium).Italic();
        });
    }

    private async Task<string> GenerateQuoteNumber()
    {
        var year = DateTime.UtcNow.Year;
        var lastQuote = await _context.Quotes
            .Where(q => q.QuoteNumber.StartsWith($"COT-{year}"))
            .OrderByDescending(q => q.Id)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastQuote != null)
        {
            var parts = lastQuote.QuoteNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"COT-{year}-{nextNumber:D4}";
    }

    private async Task<QuoteDetailDto> MapToDetailDto(Quote quote)
    {
        var user = await _userManager.FindByIdAsync(quote.CreatedByUserId);
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.QuoteId == quote.Id);

        return new QuoteDetailDto
        {
            Id = quote.Id,
            QuoteNumber = quote.QuoteNumber,
            ClientId = quote.ClientId,
            ClientName = quote.Client?.CompanyName ?? string.Empty,
            ClientEmail = quote.Client?.Email ?? string.Empty,
            CreatedAt = quote.CreatedAt,
            UpdatedAt = quote.UpdatedAt,
            ValidUntil = quote.ValidUntil,
            Status = quote.Status,
            CreatedByUserId = quote.CreatedByUserId,
            CreatedByUserName = user?.UserName ?? string.Empty,
            Subtotal = quote.Subtotal,
            Tax = quote.Tax,
            Total = quote.Total,
            Notes = quote.Notes,
            InvoiceId = invoice?.Id,
            Items = quote.Items?.Select(i => new QuoteItemDetailDto
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
            }).ToList() ?? new List<QuoteItemDetailDto>()
        };
    }
}