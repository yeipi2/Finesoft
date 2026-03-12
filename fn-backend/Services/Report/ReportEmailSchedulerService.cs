using fs_backend.DTO;
using fs_backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace fs_backend.Services;

public class ReportEmailSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportEmailSchedulerService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public ReportEmailSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<ReportEmailSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📧 ReportEmailSchedulerService iniciado - se ejecutará cada hora");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en ReportEmailSchedulerService");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessPendingEmailsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        
        var emailPrefService = scope.ServiceProvider.GetRequiredService<IReportEmailPreferenceService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        var pdfGenerator = scope.ServiceProvider.GetRequiredService<IReportPdfGenerator>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var pendingPreferences = await emailPrefService.GetPendingPreferencesAsync();

        if (pendingPreferences.Count == 0)
        {
            _logger.LogDebug("📧 No hay reportes pendientes de enviar");
            return;
        }

        _logger.LogInformation("📧 Procesando {Count} reportes pendientes", pendingPreferences.Count);

        foreach (var pref in pendingPreferences)
        {
            try
            {
                var user = await userManager.FindByIdAsync(pref.UserId);
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("⚠️ Usuario {UserId} no encontrado o sin email", pref.UserId);
                    continue;
                }

                var (startDate, endDate) = GetDateRangeForFrequency(pref.Frequency, pref.LastSentAt);

                _logger.LogInformation("📧 Generando reporte para {Email} - Período: {Start} a {End}", 
                    user.Email, startDate.ToString("dd/MM/yyyy"), endDate.ToString("dd/MM/yyyy"));

                var dashboard = pref.IncludeDashboard ? await reportService.GetDashboardStatsAsync() : null;
                var financial = pref.IncludeFinancial ? await reportService.GetFinancialReportAsync(startDate, endDate) : null;
                var performance = pref.IncludePerformance ? await reportService.GetPerformanceMetricsAsync(startDate, endDate) : null;
                var clients = pref.IncludeClients ? await reportService.GetReportsByClientAsync(startDate, endDate) : null;
                var projects = pref.IncludeProjects ? await reportService.GetReportsByProjectAsync(startDate, endDate) : null;
                var employees = pref.IncludeEmployees ? await reportService.GetReportsByUserAsync(startDate, endDate) : null;

                var pdfBytes = pdfGenerator.Generate(
                    dashboard, financial, performance, clients, projects, employees,
                    startDate, endDate);

                var emailSent = await emailService.SendReportEmailAsync(
                    user.Email,
                    user.UserName ?? "Usuario",
                    pdfBytes,
                    startDate,
                    endDate);

                if (emailSent)
                {
                    await emailPrefService.MarkAsSentAsync(pref.UserId);
                    _logger.LogInformation("✅ Reporte enviado a {Email}", user.Email);
                }
                else
                {
                    _logger.LogError("❌ Error al enviar reporte a {Email}", user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al procesar reporte para usuario {UserId}", pref.UserId);
            }
        }
    }

    private (DateTime startDate, DateTime endDate) GetDateRangeForFrequency(string frequency, DateTime? lastSentAt)
    {
        var endDate = DateTime.UtcNow;
        var startDate = frequency switch
        {
            "daily" => endDate.AddDays(-1),
            "weekly" => endDate.AddDays(-7),
            "biweekly" => endDate.AddDays(-14),
            "monthly" => endDate.AddMonths(-1),
            _ => endDate.AddDays(-7)
        };

        if (lastSentAt.HasValue && lastSentAt.Value > startDate)
        {
            startDate = lastSentAt.Value;
        }

        return (startDate, endDate);
    }
}
