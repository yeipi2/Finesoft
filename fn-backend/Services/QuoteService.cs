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

    public QuoteService(ApplicationDbContext context, UserManager<IdentityUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IEnumerable<QuoteDetailDto>> GetQuotesAsync(string? status = null, int? clientId = null)
    {
        var query = _context.Quotes
            .Include(q => q.Client)
            .Include(q => q.Items)
            // CÓDIGO FUTURO - Include de Service deshabilitado
            // .ThenInclude(i => i.Service)
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
            // CÓDIGO FUTURO - Include de Service deshabilitado
            // .ThenInclude(i => i.Service)
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
                Subtotal = itemSubtotal
                // CÓDIGO FUTURO - ServiceId deshabilitado
                // ServiceId = itemDto.ServiceId
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
                Subtotal = itemSubtotal
                // CÓDIGO FUTURO - ServiceId deshabilitado
                // ServiceId = itemDto.ServiceId
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
        _context.Quotes.Update(quote);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<byte[]> GenerateQuotePdfAsync(int id)
    {
        var quote = await _context.Quotes
            .Include(q => q.Client)
            .Include(q => q.Items)
            // CÓDIGO FUTURO - Include de Service deshabilitado
            // .ThenInclude(i => i.Service)
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

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, Quote quote)
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
                    col.Item().Text("COTIZACIÓN").FontSize(28).Bold().FontColor(purpleColor);
                    col.Item().Text(quote.QuoteNumber).FontSize(16).FontColor(Colors.Grey.Darken2);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Fecha: {quote.CreatedAt:dd/MM/yyyy}").FontSize(10);
                    if (quote.ValidUntil.HasValue)
                    {
                        col.Item().Text($"Válida hasta: {quote.ValidUntil.Value:dd/MM/yyyy}").FontSize(10);
                    }

                    var statusColor = quote.Status switch
                    {
                        "Aceptada" => Colors.Green.Medium,
                        "Rechazada" => Colors.Red.Medium,
                        "Enviada" => orangeColor,
                        _ => Colors.Grey.Medium
                    };
                    col.Item().Text($"Estado: {quote.Status}").FontSize(10).Bold().FontColor(statusColor);
                });
            });

            column.Item().PaddingTop(20).Row(row =>
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
                    col.Item().Background(orangeColor).Padding(5)
                        .Text("DETALLES DE LA COTIZACIÓN").FontSize(11).Bold().FontColor(Colors.White);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Text($"RFC: {quote.Client?.RFC}").FontSize(10);
                    col.Item().Text($"Dirección: {quote.Client?.Address}").FontSize(10);
                });
            });
        });
    }

    private void ComposeContent(IContainer container, Quote quote)
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
                    header.Cell().Background(purpleColor).Padding(8).AlignCenter().Text("Cantidad")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignRight().Text("Precio Unit.")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(purpleColor).Padding(8).AlignRight().Text("Subtotal")
                        .FontColor(Colors.White).Bold();
                });

                var isAlternate = false;
                foreach (var item in quote.Items)
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
                        row.RelativeItem().AlignRight().Text($"${quote.Subtotal:N2}").FontSize(11);
                    });

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("IVA (16%):").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text($"${quote.Tax:N2}").FontSize(11);
                    });

                    col.Item().PaddingTop(10).BorderTop(2).BorderColor(orangeColor).PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL:").FontSize(16).Bold().FontColor(purpleColor);
                        row.RelativeItem().AlignRight().Text($"${quote.Total:N2}").FontSize(16).Bold()
                            .FontColor(orangeColor);
                    });
                });
            });

            if (!string.IsNullOrEmpty(quote.Notes))
            {
                column.Item().PaddingTop(20).Border(1).BorderColor(orangeColor).Background("#FFF7ED")
                    .Padding(15).Column(col =>
                    {
                        col.Item().Text("Notas Adicionales:").Bold().FontColor(orangeColor).FontSize(12);
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
                    col.Item().Text($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("www.finesoft.com.mx").FontSize(8).FontColor(purpleColor).Underline();
                    col.Item().Text("informes@finesoft.com.mx").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            column.Item().PaddingTop(10).AlignCenter()
                .Text(
                    "Este documento es una cotización y no representa un compromiso de compra hasta su confirmación formal.")
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

        return new QuoteDetailDto
        {
            Id = quote.Id,
            QuoteNumber = quote.QuoteNumber,
            ClientId = quote.ClientId,
            ClientName = quote.Client?.CompanyName ?? string.Empty,
            ClientEmail = quote.Client?.Email ?? string.Empty,
            CreatedAt = quote.CreatedAt,
            ValidUntil = quote.ValidUntil,
            Status = quote.Status,
            CreatedByUserId = quote.CreatedByUserId,
            CreatedByUserName = user?.UserName ?? string.Empty,
            Subtotal = quote.Subtotal,
            Tax = quote.Tax,
            Total = quote.Total,
            Notes = quote.Notes,
            Items = quote.Items?.Select(i => new QuoteItemDetailDto
            {
                Id = i.Id,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                // CÓDIGO FUTURO - ServiceId y ServiceName deshabilitados
                ServiceId = null,
                ServiceName = null
                // ServiceId = i.ServiceId,
                // ServiceName = i.Service?.Name
            }).ToList() ?? new List<QuoteItemDetailDto>()
        };
    }
}