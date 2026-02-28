using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace fs_backend.Services;

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    private readonly IWebHostEnvironment _environment;

    public InvoicePdfGenerator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public byte[] Generate(Invoice invoice, string createdBy)
    {
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
                page.Footer().Element(c => ComposeFooter(c, createdBy));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, Invoice invoice)
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
                col.Item().Text("FACTURA").FontSize(28).Bold().FontColor(purpleColor);
                col.Item().Text(invoice.InvoiceNumber).FontSize(16).FontColor(Colors.Grey.Darken2);
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"Fecha: {invoice.InvoiceDate:dd/MM/yyyy}").FontSize(10);
                if (invoice.DueDate.HasValue)
                    col.Item().Text($"Vencimiento: {invoice.DueDate.Value:dd/MM/yyyy}").FontSize(10);

                var statusColor = invoice.Status switch
                {
                    InvoiceConstants.Status.Paid => Colors.Green.Medium,
                    InvoiceConstants.Status.Cancelled => Colors.Red.Medium,
                    InvoiceConstants.Status.Overdue => Colors.Red.Darken1,
                    InvoiceConstants.Status.Pending => Colors.Grey.Medium,
                    _ => Colors.Grey.Medium
                };

                col.Item().Row(r =>
                {
                    r.AutoItem().Text("Estado: ").FontSize(10).Bold().FontColor(Colors.Black);
                    r.AutoItem().Text(invoice.Status).FontSize(10).Bold().FontColor(statusColor);
                });
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        var purpleColor = "#6B46C1";
        var infoColor = "#6B46C1";

        container.PaddingVertical(20).Column(column =>
        {
            column.Item().PaddingBottom(20).Row(row =>
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
                    col.Item().Background(purpleColor).Padding(5)
                        .Text("DATOS DE FACTURACIÓN").FontSize(11).Bold().FontColor(Colors.White);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Text($"Tipo: {invoice.InvoiceType}").FontSize(10);
                    col.Item().Text($"Dirección: {invoice.Client?.Address}").FontSize(10);
                    if (!string.IsNullOrEmpty(invoice.PaymentMethod))
                        col.Item().Text($"Método de pago: {invoice.PaymentMethod}").FontSize(10);
                });
            });

            var ticketItems = invoice.Items?
                .Where(i => i.TicketId.HasValue && i.Ticket != null)
                .ToList() ?? new List<InvoiceItem>();

            if (ticketItems.Any())
            {
                column.Item().Text("TICKETS ASOCIADOS").FontSize(14).Bold().FontColor(infoColor);
                column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(infoColor);

                foreach (var item in ticketItems)
                {
                    var ticket = item.Ticket!;
                    var clientName = ticket.Project?.Client?.CompanyName ?? "N/A";
                    var projectName = ticket.Project?.Name ?? "N/A";
                    var hours = ticket.ActualHours;
                    var hourlyRate = ticket.Project?.HourlyRate ?? 0;
                    var description = ticket.Description ?? string.Empty;
                    var ticketTitle = ticket.Title ?? "Sin título";

                    column.Item().PaddingVertical(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten4).Padding(15).Column(ticketCol =>
                        {
                            ticketCol.Item().Row(row =>
                            {
                                row.ConstantItem(80).Text($"Ticket #{ticket.Id}").FontSize(12).Bold()
                                    .FontColor(infoColor);
                                row.RelativeItem().Text(ticketTitle).FontSize(12).Bold();
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
                                            infoRow.RelativeItem().AlignLeft().Text($"${ticketCost:N2}")
                                                .FontSize(10).FontColor(Colors.Green.Medium);
                                        });
                                    }
                                });
                            });

                            if (!string.IsNullOrWhiteSpace(description))
                            {
                                ticketCol.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                ticketCol.Item().PaddingTop(8).Column(descCol =>
                                {
                                    descCol.Item().Text("Descripción:").FontSize(10).Bold();
                                    descCol.Item().PaddingTop(4).Text(description).FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                });
                            }
                        });
                }

                column.Item().PaddingTop(20);
            }

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

                    col.Item().PaddingTop(10).BorderTop(2).BorderColor(Colors.Black).PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL:").FontSize(16).Bold().FontColor(Colors.Black);
                        row.RelativeItem().AlignRight().Text($"${invoice.Total:N2}").FontSize(16).Bold()
                            .FontColor(Colors.Black);
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
                                .Text(payment.Reference ?? "-").FontSize(8);
                        }
                    });
                });
            }

            if (!string.IsNullOrEmpty(invoice.Notes))
            {
                column.Item().PaddingTop(20).Border(1).BorderColor(purpleColor).Background("#F5F0FF")
                    .Padding(15).Column(col =>
                    {
                        col.Item().Text("Notas:").Bold().FontColor(purpleColor).FontSize(12);
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
}
