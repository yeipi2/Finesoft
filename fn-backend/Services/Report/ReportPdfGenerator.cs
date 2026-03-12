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
        var purple = new Color(0xFF6B46C1);
        var green = new Color(0xFF10B981);
        var yellow = new Color(0xFFF59E0B);
        var red = new Color(0xFFEF4444);
        var blue = new Color(0xFF3B82F6);

        container.PaddingVertical(15).Column(column =>
        {
            column.Item().Text("RESUMEN EJECUTIVO").FontSize(14).Bold().FontColor(purpleColor);
            column.Item().PaddingBottom(8).LineHorizontal(2).LineColor(purpleColor);

            if (dashboard != null)
            {
                column.Item().PaddingBottom(12).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Clientes Totales").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text(dashboard.TotalClients.ToString()).FontSize(20).Bold().FontColor(purple);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Proyectos Activos").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text(dashboard.ActiveProjects.ToString()).FontSize(20).Bold().FontColor(purple);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Tickets Abiertos").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text(dashboard.OpenTickets.ToString()).FontSize(20).Bold().FontColor(yellow);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Ingresos Totales").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text($"${FormatCompact(dashboard.TotalRevenue)}").FontSize(16).Bold().FontColor(green);
                    });
                });
            }

            if (performance != null)
            {
                column.Item().PaddingTop(8).Text("MÉTRICAS DE RENDIMIENTO").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(8).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tasa de Resolución").FontSize(10).Bold();
                        c.Item().Text($"{performance.TicketResolutionRate:N1}%").FontSize(16).Bold().FontColor(purple);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tiempo Promedio").FontSize(10).Bold();
                        c.Item().Text($"{performance.AverageResolutionTime:N1} hrs").FontSize(16).Bold().FontColor(blue);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tickets Resueltos").FontSize(10).Bold();
                        c.Item().Text(performance.TotalTicketsResolved.ToString()).FontSize(16).Bold().FontColor(green);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Eficiencia").FontSize(10).Bold();
                        c.Item().Text($"{(performance.BillingEfficiency * 100):N1}%").FontSize(16).Bold().FontColor(purple);
                    });
                });
            }

            if (financial != null)
            {
                column.Item().PaddingTop(20).Text("RESUMEN FINANCIERO").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(8).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(purple).Padding(6).Text("Concepto").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Monto").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignCenter().Text("Cant.").FontColor(Colors.White).Bold();
                    });

                    var items = new (string, decimal, int, Color)[]
                    {
                        ("Total Facturado", financial.TotalInvoiced, financial.InvoicesCount, purple),
                        ("Total Pagado", financial.TotalPaid, financial.PaidInvoicesCount, green),
                        ("Pendiente", financial.TotalPending, financial.PendingInvoicesCount, yellow),
                        ("Vencido", financial.TotalOverdue, 0, red)
                    };

                    var isAlternate = false;
                    foreach (var item in items)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(item.Item1).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"${item.Item2:N2}").FontColor(item.Item4).Bold().FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter().Text(item.Item3.ToString()).FontSize(10);
                        isAlternate = !isAlternate;
                    }
                });

                column.Item().PaddingTop(15).Text("Distribución de Pagos").FontSize(12).Bold().FontColor(purpleColor);
                column.Item().PaddingTop(8).Column(c =>
                {
                    var total = financial.TotalInvoiced > 0 ? financial.TotalInvoiced : 1;
                    var paid = financial.TotalPaid / total * 100;
                    var pending = financial.TotalPending / total * 100;
                    var overdue = financial.TotalOverdue / total * 100;

                    c.Item().PaddingBottom(8).Row(r =>
                    {
                        r.RelativeItem(2).Text("Pagado").FontSize(9);
                        r.ConstantItem(40).Text($"{paid:N0}%").FontSize(9).Bold().FontColor(green);
                        r.RelativeItem(3).Background(green).Height(10);
                    });
                    c.Item().PaddingBottom(8).Row(r =>
                    {
                        r.RelativeItem(2).Text("Pendiente").FontSize(9);
                        r.ConstantItem(40).Text($"{pending:N0}%").FontSize(9).Bold().FontColor(yellow);
                        r.RelativeItem(3).Background(yellow).Height(10);
                    });
                    c.Item().Row(r =>
                    {
                        r.RelativeItem(2).Text("Vencido").FontSize(9);
                        r.ConstantItem(40).Text($"{overdue:N0}%").FontSize(9).Bold().FontColor(red);
                        r.RelativeItem(3).Background(red).Height(10);
                    });
                });

                column.Item().PaddingTop(15).Text("Totales").FontSize(12).Bold().FontColor(purpleColor);
                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Facturado:").FontSize(9).FontColor(Colors.Grey.Medium);
                            r.RelativeItem().AlignRight().Text($"${financial.TotalInvoiced:N2}").FontSize(10).Bold().FontColor(purple);
                        });
                        c.Item().PaddingBottom(4);

                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Pagado:").FontSize(9).FontColor(Colors.Grey.Medium);
                            r.RelativeItem().AlignRight().Text($"${financial.TotalPaid:N2}").FontSize(10).Bold().FontColor(green);
                        });
                        c.Item().PaddingBottom(4);

                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Pendiente:").FontSize(9).FontColor(Colors.Grey.Medium);
                            r.RelativeItem().AlignRight().Text($"${financial.TotalPending:N2}").FontSize(10).Bold().FontColor(yellow);
                        });
                        c.Item().PaddingBottom(4);

                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Vencido:").FontSize(9).FontColor(Colors.Grey.Medium);
                            r.RelativeItem().AlignRight().Text($"${financial.TotalOverdue:N2}").FontSize(10).Bold().FontColor(red);
                        });
                    });
                });
            }

            if (clients != null && clients.Any())
            {
                column.Item().PaddingTop(20).Text("TOP 10 CLIENTES").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(8).LineHorizontal(2).LineColor(purpleColor);

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
                        header.Cell().Background(purple).Padding(6).Text("Cliente").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Tickets").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Facturado").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Pagado").FontColor(Colors.White).Bold();
                    });

                    var topClients = clients.OrderByDescending(c => c.TotalBilled).Take(10).ToList();
                    var isAlternate = false;
                    foreach (var client in topClients)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(client.ClientName).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(client.TotalTickets.ToString()).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"${client.TotalBilled:N0}").FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"${client.TotalPaid:N0}").FontSize(10);
                        isAlternate = !isAlternate;
                    }
                });

                if (clients.Count > 0)
                {
                    var top5Clients = clients.OrderByDescending(c => c.TotalBilled).Take(5).ToList();
                    var maxValue = top5Clients.Max(c => c.TotalBilled);

                    column.Item().PaddingTop(12).Text("Top 5 Clientes por Ingresos").FontSize(11).Bold().FontColor(purpleColor);
                    column.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            var colors = new[] { purple, blue, green, yellow, red };
                            for (int i = 0; i < top5Clients.Count; i++)
                            {
                                var client = top5Clients[i];
                                var barWidth = maxValue > 0 ? (float)(client.TotalBilled / maxValue) : 0;
                                var color = colors[i % colors.Length];

                                c.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.RelativeItem().Text(client.ClientName.Length > 15 ? client.ClientName.Substring(0, 15) : client.ClientName).FontSize(9);
                                    r.ConstantItem(55).AlignRight().Text($"${client.TotalBilled:N0}").FontSize(9).Bold();
                                });
                                c.Item().Background(color).Height(12).Width(barWidth).MinWidth(15);
                                c.Item().PaddingBottom(8);
                            }
                        });
                    });
                }
            }

            if (projects != null && projects.Any())
            {
                column.Item().PaddingTop(20).Text("PROYECTOS").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(8).LineHorizontal(2).LineColor(purpleColor);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(purple).Padding(6).Text("Proyecto").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).Text("Cliente").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Tickets").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Horas").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Ingresos").FontColor(Colors.White).Bold();
                    });

                    var topProjects = projects.OrderByDescending(p => p.Revenue).Take(10).ToList();
                    var isAlternate = false;
                    foreach (var project in topProjects)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(project.ProjectName).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(project.ClientName).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(project.TotalTickets.ToString()).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{project.TotalHours:N1}").FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"${project.Revenue:N0}").FontSize(10).FontColor(green);
                        isAlternate = !isAlternate;
                    }
                });

                if (projects.Count > 0)
                {
                    var top5Projects = projects.OrderByDescending(p => p.Revenue).Take(5).ToList();
                    var maxValue = top5Projects.Max(p => p.Revenue);

                    column.Item().PaddingTop(12).Text("Top 5 Proyectos por Ingresos").FontSize(11).Bold().FontColor(purpleColor);
                    column.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            var colors = new[] { green, blue, purple, yellow, red };
                            for (int i = 0; i < top5Projects.Count; i++)
                            {
                                var project = top5Projects[i];
                                var barWidth = maxValue > 0 ? (float)(project.Revenue / maxValue) : 0;
                                var color = colors[i % colors.Length];

                                c.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.RelativeItem().Text(project.ProjectName.Length > 15 ? project.ProjectName.Substring(0, 15) : project.ProjectName).FontSize(9);
                                    r.ConstantItem(55).AlignRight().Text($"${project.Revenue:N0}").FontSize(9).Bold();
                                });
                                c.Item().Background(color).Height(12).Width(barWidth).MinWidth(15);
                                c.Item().PaddingBottom(8);
                            }
                        });
                    });
                }
            }

            if (employees != null && employees.Any())
            {
                column.Item().PaddingTop(20).Text("RENDIMIENTO DE EMPLEADOS").FontSize(14).Bold().FontColor(purpleColor);
                column.Item().PaddingBottom(8).LineHorizontal(2).LineColor(purpleColor);

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
                        header.Cell().Background(purple).Padding(6).Text("Empleado").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Creados").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Cerrados").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Horas").FontColor(Colors.White).Bold();
                        header.Cell().Background(purple).Padding(6).AlignRight().Text("Tasa").FontColor(Colors.White).Bold();
                    });

                    var topEmployees = employees.OrderByDescending(e => e.TicketsCreated).Take(10).ToList();
                    var isAlternate = false;
                    foreach (var emp in topEmployees)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        var rate = emp.TicketsCreated > 0 ? (decimal)emp.TicketsClosed / emp.TicketsCreated * 100 : 0;
                        var rateColor = rate >= 80 ? green : (rate >= 50 ? yellow : red);

                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(emp.UserName).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(emp.TicketsCreated.ToString()).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text(emp.TicketsClosed.ToString()).FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{emp.TotalHoursWorked:N1}").FontSize(10);
                        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{rate:N0}%").FontSize(10).Bold().FontColor(rateColor);
                        isAlternate = !isAlternate;
                    }
                });

                if (employees.Count > 0)
                {
                    var top5Employees = employees.OrderByDescending(e => e.TicketsCreated).Take(5).ToList();
                    var maxTickets = top5Employees.Max(e => e.TicketsCreated);

                    column.Item().PaddingTop(12).Text("Top 5 Empleados por Tickets").FontSize(11).Bold().FontColor(purpleColor);
                    column.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            var colors = new[] { blue, green, purple, yellow, red };
                            for (int i = 0; i < top5Employees.Count; i++)
                            {
                                var emp = top5Employees[i];
                                var barWidth = maxTickets > 0 ? (float)emp.TicketsCreated / maxTickets : 0;
                                var color = colors[i % colors.Length];

                                c.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.RelativeItem().Text(emp.UserName.Length > 15 ? emp.UserName.Substring(0, 15) : emp.UserName).FontSize(9);
                                    r.ConstantItem(30).AlignRight().Text(emp.TicketsCreated.ToString()).FontSize(9).Bold();
                                });
                                c.Item().Background(color).Height(12).Width(barWidth).MinWidth(15);
                                c.Item().PaddingBottom(8);
                            }
                        });
                    });
                }
            }
        });
    }

    private string FormatCompact(decimal value)
    {
        if (value >= 1000000)
            return $"{value / 1000000:N1}M";
        if (value >= 1000)
            return $"{value / 1000:N1}K";
        return value.ToString("N0");
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
