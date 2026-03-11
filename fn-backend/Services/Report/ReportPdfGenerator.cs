using fs_backend.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace fs_backend.Services;

public class ReportPdfGenerator : IReportPdfGenerator
{
    private readonly IWebHostEnvironment _environment;

    public ReportPdfGenerator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public byte[] Generate(
        DashboardStatsDto? dashboard,
        FinancialReportDto? financial,
        PerformanceMetricsDto? performance,
        List<ClientReportDto>? clients,
        List<ProjectReportDto>? projects,
        List<UserReportDto>? employees,
        DateTime startDate,
        DateTime endDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, startDate, endDate));
                page.Content().Element(c => ComposeContent(c, dashboard, financial, performance, clients, projects, employees));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, DateTime startDate, DateTime endDate)
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
                col.Item().Text("REPORTE CONSOLIDADO").FontSize(22).Bold().FontColor(purpleColor);
                col.Item().Text($"Período: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}").FontSize(12).FontColor(Colors.Grey.Darken2);
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
            });
        });
    }

    private void ComposeContent(IContainer container,
        DashboardStatsDto? dashboard,
        FinancialReportDto? financial,
        PerformanceMetricsDto? performance,
        List<ClientReportDto>? clients,
        List<ProjectReportDto>? projects,
        List<UserReportDto>? employees)
    {
        var purpleColor = "#6B46C1";
        var infoColor = "#6B46C1";

        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Text("RESUMEN EJECUTIVO").FontSize(14).Bold().FontColor(purpleColor);
            column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(purpleColor);

            if (dashboard != null)
            {
                column.Item().PaddingBottom(15).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Clientes Totales").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text(dashboard.TotalClients.ToString()).FontSize(20).Bold().FontColor(purpleColor);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Proyectos Activos").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text(dashboard.ActiveProjects.ToString()).FontSize(20).Bold().FontColor(purpleColor);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Tickets Abiertos").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text(dashboard.OpenTickets.ToString()).FontSize(20).Bold().FontColor(purpleColor);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Ingresos Totales").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text($"${dashboard.TotalRevenue:N2}").FontSize(20).Bold().FontColor(Colors.Green.Medium);
                    });
                });
            }

            if (financial != null)
            {
                column.Item().PaddingTop(10).Text("RESUMEN FINANCIERO").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(purpleColor).Padding(8).Text("Concepto").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(8).AlignRight().Text("Monto").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(8).AlignCenter().Text("Cantidad").FontColor(Colors.White).Bold();
                    });

                    var items = new[]
                    {
                        ("Total Facturado", financial.TotalInvoiced, financial.InvoicesCount),
                        ("Total Pagado", financial.TotalPaid, financial.PaidInvoicesCount),
                        ("Pendiente", financial.TotalPending, financial.PendingInvoicesCount),
                        ("Vencido", financial.TotalOverdue, 0)
                    };

                    var isAlternate = false;
                    foreach (var item in items)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(item.Item1);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignRight().Text($"${item.Item2:N2}");
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignCenter().Text(item.Item3.ToString());
                        isAlternate = !isAlternate;
                    }
                });
            }

            if (performance != null)
            {
                column.Item().PaddingTop(20).Text("MÉTRICAS DE RENDIMIENTO").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tasa de Resolución").FontSize(10).Bold();
                        c.Item().Text($"{performance.TicketResolutionRate:N1}%").FontSize(16).FontColor(infoColor);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tiempo Promedio de Resolución").FontSize(10).Bold();
                        c.Item().Text($"{performance.AverageResolutionTime:N1} hrs").FontSize(16).FontColor(infoColor);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tickets Resueltos").FontSize(10).Bold();
                        c.Item().Text(performance.TotalTicketsResolved.ToString()).FontSize(16).FontColor(infoColor);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Eficiencia de Facturación").FontSize(10).Bold();
                        c.Item().Text($"{(performance.BillingEfficiency * 100):N1}%").FontSize(16).FontColor(infoColor);
                    });
                });
            }

            if (clients != null && clients.Any())
            {
                column.Item().PaddingTop(20).Text("TOP 10 CLIENTES").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(purpleColor).Padding(6).Text("Cliente").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Tickets").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Facturado").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Pagado").FontColor(Colors.White).Bold();
                    });

                    var topClients = clients.OrderByDescending(c => c.TotalBilled).Take(10).ToList();
                    var isAlternate = false;
                    foreach (var client in topClients)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(client.ClientName);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(client.TotalTickets.ToString());
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"${client.TotalBilled:N2}");
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"${client.TotalPaid:N2}");
                        isAlternate = !isAlternate;
                    }
                });
            }

            if (projects != null && projects.Any())
            {
                column.Item().PaddingTop(20).Text("PROYECTOS").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(purpleColor).Padding(6).Text("Proyecto").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).Text("Cliente").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Tickets").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Horas").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Ingresos").FontColor(Colors.White).Bold();
                    });

                    var topProjects = projects.OrderByDescending(p => p.Revenue).Take(10).ToList();
                    var isAlternate = false;
                    foreach (var project in topProjects)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(project.ProjectName);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(project.ClientName);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(project.TotalTickets.ToString());
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{project.TotalHours:N1}");
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"${project.Revenue:N2}");
                        isAlternate = !isAlternate;
                    }
                });
            }

            if (employees != null && employees.Any())
            {
                column.Item().PaddingTop(20).Text("RENDIMIENTO DE EMPLEADOS").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(10).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(purpleColor).Padding(6).Text("Empleado").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Creados").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Cerrados").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Horas").FontColor(Colors.White).Bold();
                        header.Cell().Background(purpleColor).Padding(6).AlignRight().Text("Tasa").FontColor(Colors.White).Bold();
                    });

                    var topEmployees = employees.OrderByDescending(e => e.TicketsCreated).Take(10).ToList();
                    var isAlternate = false;
                    foreach (var emp in topEmployees)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        var rate = emp.TicketsCreated > 0 ? (decimal)emp.TicketsClosed / emp.TicketsCreated * 100 : 0;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(emp.UserName);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(emp.TicketsCreated.ToString());
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(emp.TicketsClosed.ToString());
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{emp.TotalHoursWorked:N1}");
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{rate:N0}%");
                        isAlternate = !isAlternate;
                    }
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
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
                    col.Item().Text($"Página 1").FontSize(8).FontColor(Colors.Grey.Medium);
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

public interface IReportPdfGenerator
{
    byte[] Generate(
        DashboardStatsDto? dashboard,
        FinancialReportDto? financial,
        PerformanceMetricsDto? performance,
        List<ClientReportDto>? clients,
        List<ProjectReportDto>? projects,
        List<UserReportDto>? employees,
        DateTime startDate,
        DateTime endDate);
}
