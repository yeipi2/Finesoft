using fs_backend.Services;
using fs_backend.Identity;
using fn_backend.Models;
using fs_backend.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace fn_backend.Jobs;

/// <summary>
/// Job que verifica facturas próximas a vencer y envía notificaciones.
/// Se ejecuta diariamente.
/// </summary>
public class InvoiceExpirationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvoiceExpirationJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Ejecutar diariamente

    public InvoiceExpirationJob(
        IServiceProvider serviceProvider,
        ILogger<InvoiceExpirationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Job de facturación: Verificación de facturas próximas a vencer iniciada");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiringInvoicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al verificar facturas próximas a vencer");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckExpiringInvoicesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationHelper = scope.ServiceProvider.GetRequiredService<INotificationHelper>();

        // Facturas pendientes que vencen en los próximos 3 días
        var threeDaysFromNow = DateTime.UtcNow.AddDays(3).Date;
        var today = DateTime.UtcNow.Date;

        var expiringInvoices = await context.Invoices
            .Include(i => i.Client)
            .Where(i =>
                i.Status == InvoiceConstants.Status.Pending &&
                i.DueDate.HasValue &&
                i.DueDate.Value.Date <= threeDaysFromNow &&
                i.DueDate.Value.Date >= today)
            .ToListAsync();

        if (expiringInvoices.Any())
        {
            _logger.LogInformation(
                "📋 Se encontraron {Count} facturas próximas a vencer",
                expiringInvoices.Count);

            foreach (var invoice in expiringInvoices)
            {
                var daysUntilDue = (invoice.DueDate!.Value.Date - DateTime.UtcNow.Date).Days;

                var notification = notificationHelper.CreateNotification(
                    NotificationType.InvoiceExpiringSoon,
                    "Factura Próxima a Vencer",
                    $"La factura #{invoice.InvoiceNumber} para {invoice.Client?.CompanyName} vence en {daysUntilDue} día(s)",
                    $"/invoices/{invoice.Id}");

                await notificationHelper.SendToAdminsAsync(notification);
                await notificationHelper.SendToAdministracionAsync(notification);

                _logger.LogInformation(
                    "🔔 Notificación enviada para factura #{InvoiceNumber} - vence en {Days} días",
                    invoice.InvoiceNumber,
                    daysUntilDue);
            }
        }
        else
        {
            _logger.LogInformation("✅ No hay facturas próximas a vencer");
        }
    }
}

/// <summary>
/// Extensiones para registrar el job en DI
/// </summary>
public static class InvoiceExpirationJobExtensions
{
    public static IServiceCollection AddInvoiceExpirationJob(this IServiceCollection services)
    {
        services.AddHostedService<InvoiceExpirationJob>();
        return services;
    }
}