using Asp.Versioning;
using fs_backend.DTO;
using fs_backend.Attributes;
using fs_backend.DTO.Common;
using fn_backend.DTO;
using fs_backend.Hubs;
using fs_backend.Identity;
using fs_backend.Repositories;
using fs_backend.Services;
using fs_backend.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace fs_backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ILogger<QuotesController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<QuotesHub> _quotesHub;
    private readonly IHubContext<NotificationsHub> _notificationsHub;
    private readonly INotificationService _notificationService;
    private readonly UserManager<IdentityUser> _userManager;

    public QuotesController(
        IQuoteService quoteService,
        ILogger<QuotesController> logger,
        ApplicationDbContext context,
        IHubContext<QuotesHub> quotesHub,
        IHubContext<NotificationsHub> notificationsHub,
        INotificationService notificationService,
        UserManager<IdentityUser> userManager)
    {
        _quoteService = quoteService;
        _logger = logger;
        _context = context;
        _quotesHub = quotesHub;
        _notificationsHub = notificationsHub;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    /// <summary>
    /// GET: api/quotes
    /// </summary>
    [HttpGet]
    [RequirePermission("quotes.view")]
    public async Task<IActionResult> GetQuotes(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] string? status = null,
        [FromQuery] int? clientId = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo cotizaciones", userId);

        var sortDescending = string.IsNullOrEmpty(query.Sort) || !query.Sort.StartsWith("-");
        var sortField = sortDescending ? query.Sort : query.Sort.Substring(1);

        var (quotes, total) = await _quoteService.GetQuotesPaginatedAsync(
            search: query.Search,
            status: status,
            clientId: clientId,
            sortField: string.IsNullOrEmpty(sortField) ? "createdAt" : sortField,
            sortDescending: sortDescending,
            page: query.NormalizedPage,
            pageSize: query.NormalizedPageSize
        );

        var pagedResult = PaginatedResponseDto<QuoteDetailDto>.Create(quotes, total, query.NormalizedPage, query.NormalizedPageSize);
        return Ok(pagedResult);
    }

    /// <summary>
    /// GET: api/quotes/{id}
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("quotes.view_detail")]
    public async Task<IActionResult> GetQuoteById(int id)
    {
        var quote = await _quoteService.GetQuoteByIdAsync(id);
        if (quote == null)
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada");
        return Ok(quote);
    }

    /// <summary>
    /// POST: api/quotes
    /// </summary>
    [HttpPost]
    [RequirePermission("quotes.create")]
    public async Task<IActionResult> CreateQuote(QuoteDto quoteDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");

        _logger.LogInformation("✅ Usuario {UserId} creando cotización", userId);
        var result = await _quoteService.CreateQuoteAsync(quoteDto, userId);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);

        return CreatedAtAction(nameof(GetQuoteById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/quotes/{id}
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("quotes.edit")]
    public async Task<IActionResult> UpdateQuote(int id, QuoteDto quoteDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} actualizando cotización {QuoteId}", userId, id);
        var result = await _quoteService.UpdateQuoteAsync(id, quoteDto);
        if (!result.Succeeded)
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", result.Errors.FirstOrDefault() ?? "Cotización no encontrada");
        return NoContent();
    }

    /// <summary>
    /// DELETE: api/quotes/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("quotes.delete")]
    public async Task<IActionResult> DeleteQuote(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} eliminando cotización {QuoteId}", userId, id);
        var result = await _quoteService.DeleteQuoteAsync(id);
        if (!result.Succeeded)
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", result.Errors.FirstOrDefault() ?? "Cotización no encontrada");
        return NoContent();
    }

    /// <summary>
    /// PATCH: api/quotes/{id}/status
    /// </summary>
    [HttpPatch("{id}/status")]
    [RequirePermission("quotes.edit")]
    public async Task<IActionResult> ChangeQuoteStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        try
        {
            _logger.LogInformation("🔄 Cambiando estado de cotización {QuoteId} a {NewStatus}", id, request.Status);

            // Obtener info ANTES de cambiar para tener los datos del SignalR
            var quoteInfo = await _quoteService.GetQuoteByIdAsync(id);
            if (quoteInfo == null)
                return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada");

            var result = await _quoteService.ChangeQuoteStatusAsync(id, request.Status);
            if (!result.Succeeded)
            {
                _logger.LogWarning("❌ Error al cambiar estado: {Errors}", string.Join(", ", result.Errors));
                return this.ToValidationProblem(result.Errors);
            }

            // ✅ PascalCase para que el record de C# en el frontend pueda deserializar correctamente
            await _quotesHub.Clients.Group("internal-users").SendAsync("QuoteStatusChanged", new
            {
                QuoteId = quoteInfo.Id,
                QuoteNumber = quoteInfo.QuoteNumber,
                ClientName = quoteInfo.ClientName,
                NewStatus = request.Status
            });

            _logger.LogInformation("✅ Estado cambiado y SignalR enviado para cotización {QuoteId}", id);
            return Ok(new { message = "Estado actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al cambiar estado de cotización {QuoteId}: {Message}", id, ex.Message);
            return this.ToProblem(StatusCodes.Status500InternalServerError, "Internal server error", ex.Message);
        }
    }

    /// <summary>
    /// POST: api/quotes/{id}/send-email
    /// </summary>
    [HttpPost("{id}/send-email")]
    [RequirePermission("quotes.edit")]
    public async Task<IActionResult> SendQuoteEmail(int id)
    {
        try
        {
            _logger.LogInformation("📧 Iniciando envío de email para cotización {QuoteId}", id);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("👤 Usuario autenticado: {UserId}", userId);

            var quote = await _context.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                _logger.LogWarning("❌ Cotización {QuoteId} no encontrada", id);
                return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada");
            }

            _logger.LogInformation("📋 Cotización {QuoteId} - Estado: {Status}, Cliente: {ClientEmail}",
                id, quote.Status, quote.Client?.Email);

            if (string.IsNullOrEmpty(quote.Client?.Email))
            {
                _logger.LogWarning("❌ Cliente sin email para cotización {QuoteId}", id);
                return this.ToValidationProblem(new[] { "El cliente no tiene email registrado" });
            }

            var pdfBytes = await _quoteService.GenerateQuotePdfAsync(id);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("❌ No se pudo generar PDF para cotización {QuoteId}", id);
                return this.ToValidationProblem(new[] { "No se pudo generar el PDF" });
            }

            if (string.IsNullOrEmpty(quote.PublicToken))
            {
                quote.PublicToken = Guid.NewGuid().ToString("N");
                _context.Quotes.Update(quote);
                await _context.SaveChangesAsync();
            }

            var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
            _logger.LogInformation("📧 Enviando email a {Email}...", quote.Client.Email);

            var emailSent = await emailService.SendQuoteEmailAsync(
                quote.Client.Email,
                quote.Client.CompanyName,
                quote.QuoteNumber,
                pdfBytes,
                quote.PublicToken
            );

            if (!emailSent)
            {
                _logger.LogError("❌ Error al enviar email para cotización {QuoteId}", id);
                return this.ToProblem(StatusCodes.Status500InternalServerError, "Internal server error", "Error al enviar el correo electrónico");
            }

            quote.Status = "Enviada";
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();

            // ✅ PascalCase + solo notificar cambio silencioso (sin emoji de cliente)
            await _quotesHub.Clients.Group("internal-users").SendAsync("QuoteStatusChanged", new
            {
                QuoteId = quote.Id,
                QuoteNumber = quote.QuoteNumber,
                ClientName = quote.Client.CompanyName,
                NewStatus = "Enviada"
            });

            // Notificar a administración que se envió una cotización
            var adminNotification = new NotificationDto
            {
                Type = "quote_sent",
                Title = "Cotización Enviada",
                Message = $"Cotización {quote.QuoteNumber} enviada a {quote.Client.CompanyName}",
                Link = $"/cotizaciones/{quote.Id}",
                IconClass = "bi bi-envelope text-primary",
                IconColor = "#3B82F6"
            };
            // Notificar a Admin y Administracion (evitar duplicados)
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUsers2 = await _userManager.GetUsersInRoleAsync("Administracion");
            var allAdminUsers = adminUsers.Concat(adminUsers2).DistinctBy(u => u.Id).ToList();

            foreach (var user in allAdminUsers)
            {
                await _notificationService.SaveNotificationAsync(user.Id, adminNotification);
                await NotificationsHub.SendToUser(_notificationsHub, user.Id, adminNotification);
            }

            // Guardar notificación para el usuario actual (para persistencia)
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserId) && !allAdminUsers.Any(u => u.Id == currentUserId))
            {
                await _notificationService.SaveNotificationAsync(currentUserId, adminNotification);
            }

            // Notificar al cliente si tiene cuenta en el sistema (UserId no null)
            if (!string.IsNullOrEmpty(quote.Client.UserId))
            {
                var clientNotification = new NotificationDto
                {
                    Type = "quote_received",
                    Title = "Nueva Cotización Recibida",
                    Message = $"Has recibido una cotización ({quote.QuoteNumber}) de FINESOFT. Revisa tu correo para más detalles.",
                    Link = $"/cotizaciones/{quote.Id}",
                    IconClass = "bi bi-envelope-open text-success",
                    IconColor = "#10B981"
                };
                await _notificationService.SaveNotificationAsync(quote.Client.UserId, clientNotification);
                await NotificationsHub.SendToUser(_notificationsHub, quote.Client.UserId, clientNotification);
            }

            _logger.LogInformation("✅ Email enviado, estado actualizado y SignalR enviado para cotización {QuoteId}", id);
            return Ok(new { message = "Cotización enviada exitosamente por correo electrónico", newStatus = "Enviada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email para cotización {QuoteId}: {Message}", id, ex.Message);
            return this.ToProblem(StatusCodes.Status500InternalServerError, "Internal server error", "Error al enviar la cotización: " + ex.Message);
        }
    }

    /// <summary>
    /// GET: api/quotes/{id}/pdf
    /// </summary>
    [HttpGet("{id}/pdf")]
    [RequirePermission("quotes.view_detail")]
    public async Task<IActionResult> GetQuotePdf(int id)
    {
        try
        {
            var pdfBytes = await _quoteService.GenerateQuotePdfAsync(id);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return this.ToValidationProblem(new[] { "No se pudo generar el PDF" });

            var cd = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline")
            {
                FileName = $"Cotizacion-{id}.pdf"
            };
            Response.Headers.Append("Content-Disposition", cd.ToString());
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            return File(pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generando PDF para cotización {QuoteId}", id);
            return this.ToProblem(StatusCodes.Status500InternalServerError, "Internal server error", "Error generando PDF");
        }
    }

    /// <summary>
    /// GET: api/quotes/public/{token}
    /// ⭐ PÚBLICO
    /// </summary>
    [HttpGet("public/{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicApi")]
    public async Task<IActionResult> GetQuoteByPublicToken(string token)
    {
        try
        {
            var quote = await _quoteService.GetQuoteByPublicTokenAsync(token);
            if (quote == null)
                return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada o enlace inválido");
            return Ok(quote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cotización por token público");
            return this.ToValidationProblem(new[] { ex.Message });
        }
    }

    /// <summary>
    /// POST: api/quotes/public/{token}/respond
    /// ⭐ PÚBLICO - Acepta o rechaza la cotización y notifica via SignalR
    /// </summary>
    [HttpPost("public/{token}/respond")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicApi")]
    public async Task<IActionResult> RespondToQuote(string token, [FromBody] RespondQuoteDto dto)
    {
        return await HandlePublicQuoteResponse(token, dto.Status, dto.Comments);
    }

    /// <summary>
    /// PATCH: api/quotes/public/{token}/status
    /// </summary>
    [HttpPatch("public/{token}/status")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicApi")]
    public async Task<IActionResult> UpdatePublicQuoteStatus(string token, [FromBody] RespondQuoteDto dto)
    {
        return await HandlePublicQuoteResponse(token, dto.Status, dto.Comments);
    }

    /// <summary>
    /// POST: api/quotes/public/by-number/{quoteNumber}/status
    /// </summary>
    [HttpPost("public/by-number/{quoteNumber}/status")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicApi")]
    public async Task<IActionResult> UpdatePublicQuoteStatusByNumber(
        string quoteNumber,
        [FromForm] string status,
        [FromForm] string? comments = null)
    {
        var quote = await _context.Quotes
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.QuoteNumber == quoteNumber);

        if (quote == null)
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada");

        if (string.IsNullOrWhiteSpace(quote.PublicToken))
        {
            quote.PublicToken = Guid.NewGuid().ToString("N");
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();
        }

        var result = await HandlePublicQuoteResponse(quote.PublicToken, status, comments);
        if (result is ObjectResult objectResult && objectResult.StatusCode is >= 400)
            return result;

        var html = string.Equals(status, "Aceptada", StringComparison.OrdinalIgnoreCase)
            ? GetAcceptedHtml(quote.QuoteNumber, quote.Client?.CompanyName ?? "Cliente")
            : GetRejectedHtml(quote.QuoteNumber, quote.Client?.CompanyName ?? "Cliente");

        return Content(html, "text/html");
    }

    /// <summary>
    /// GET: api/quotes/accept/{quoteNumber}
    /// </summary>
    [Obsolete("Legacy endpoint. Use PATCH /api/v1/quotes/public/{token}/status")]
    [HttpGet("accept/{quoteNumber}")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicApi")]
    public async Task<IActionResult> AcceptQuoteFromEmail(string quoteNumber)
    {
        try
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.QuoteNumber == quoteNumber);

            if (quote == null)
                return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada");

            Response.Headers.Append("Deprecation", "true");
            Response.Headers.Append("Sunset", "Tue, 30 Jun 2026 00:00:00 GMT");

            var postUrl = $"{Request.Scheme}://{Request.Host}/api/v1/quotes/public/by-number/{Uri.EscapeDataString(quoteNumber)}/status";
            return Content(GetLegacyConfirmationHtml(quoteNumber, quote.Client?.CompanyName ?? "Cliente", "Aceptada", postUrl), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al aceptar cotización {QuoteNumber}", quoteNumber);
            return Content(GetErrorHtml(), "text/html");
        }
    }

    /// <summary>
    /// GET: api/quotes/reject/{quoteNumber}
    /// </summary>
    [Obsolete("Legacy endpoint. Use PATCH /api/v1/quotes/public/{token}/status")]
    [HttpGet("reject/{quoteNumber}")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicApi")]
    public async Task<IActionResult> RejectQuoteFromEmail(string quoteNumber)
    {
        try
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.QuoteNumber == quoteNumber);

            if (quote == null)
                return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada");

            Response.Headers.Append("Deprecation", "true");
            Response.Headers.Append("Sunset", "Tue, 30 Jun 2026 00:00:00 GMT");

            var postUrl = $"{Request.Scheme}://{Request.Host}/api/v1/quotes/public/by-number/{Uri.EscapeDataString(quoteNumber)}/status";
            return Content(GetLegacyConfirmationHtml(quoteNumber, quote.Client?.CompanyName ?? "Cliente", "Rechazada", postUrl), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al rechazar cotización {QuoteNumber}", quoteNumber);
            return Content(GetErrorHtml(), "text/html");
        }
    }

    private async Task<IActionResult> HandlePublicQuoteResponse(string token, string status, string? comments)
    {
        try
        {
            var quoteInfo = await _quoteService.GetQuoteByPublicTokenAsync(token);
            if (quoteInfo == null)
                return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cotización no encontrada");

            var result = await _quoteService.RespondToQuoteAsync(token, status, comments);
            if (!result.Succeeded)
                return this.ToValidationProblem(result.Errors);

            // ✅ PascalCase para deserialización correcta en el frontend
            await _quotesHub.Clients.Group("internal-users").SendAsync("QuoteStatusChanged", new
            {
                QuoteId = quoteInfo.Id,
                QuoteNumber = quoteInfo.QuoteNumber,
                ClientName = quoteInfo.ClientName,
                NewStatus = status
            });

            var isAccepted = status.Equals("Aceptada", StringComparison.OrdinalIgnoreCase);
            
            // Obtener usuarios Admin y Administracion para evitar duplicados
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUsers2 = await _userManager.GetUsersInRoleAsync("Administracion");
            var allAdminUsers = adminUsers.Concat(adminUsers2).Distinct().ToList();
            var adminUserIds = allAdminUsers.Select(u => u.Id).ToHashSet();

            var notification = new NotificationDto
            {
                Type = isAccepted ? "quote_accepted" : "quote_rejected",
                Title = isAccepted ? "Cotización Aceptada" : "Cotización Rechazada",
                Message = $"El cliente {quoteInfo.ClientName} ha {(isAccepted ? "aceptado" : "rechazado")} la cotización {quoteInfo.QuoteNumber}",
                Link = $"/cotizaciones/{quoteInfo.Id}",
                IconClass = isAccepted ? "bi bi-check-circle text-success" : "bi bi-x-circle text-danger",
                IconColor = isAccepted ? "#10B981" : "#EF4444"
            };
            
            // Guardar y enviar a Admin y Administracion (evitar duplicados)
            foreach (var user in allAdminUsers)
            {
                await _notificationService.SaveNotificationAsync(user.Id, notification);
                await NotificationsHub.SendToUser(_notificationsHub, user.Id, notification);
            }

            // Solo enviar notificación separada al creador si NO es Admin ni Administración
            if (!string.IsNullOrEmpty(quoteInfo.CreatedByUserId) && !adminUserIds.Contains(quoteInfo.CreatedByUserId))
            {
                var creatorNotification = new NotificationDto
                {
                    Type = isAccepted ? "quote_accepted" : "quote_rejected",
                    Title = isAccepted ? "Tu Cotización Fue Aceptada" : "Tu Cotización Fue Rechazada",
                    Message = $"El cliente {quoteInfo.ClientName} ha {(isAccepted ? "aceptado" : "rechazado")} tu cotización {quoteInfo.QuoteNumber}",
                    Link = $"/cotizaciones/{quoteInfo.Id}",
                    IconClass = isAccepted ? "bi bi-check-circle text-success" : "bi bi-x-circle text-danger",
                    IconColor = isAccepted ? "#10B981" : "#EF4444"
                };
                await _notificationService.SaveNotificationAsync(quoteInfo.CreatedByUserId, creatorNotification);
                await NotificationsHub.SendToUser(_notificationsHub, quoteInfo.CreatedByUserId, creatorNotification);
            }

            _logger.LogInformation("📡 SignalR: cotización {QuoteNumber} → {Status}", quoteInfo.QuoteNumber, status);

            return Ok(new
            {
                message = "Respuesta registrada correctamente",
                quoteId = quoteInfo.Id,
                quoteNumber = quoteInfo.QuoteNumber,
                status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al responder cotización");
            return this.ToValidationProblem(new[] { ex.Message });
        }
    }

    private static string GetLegacyConfirmationHtml(string quoteNumber, string clientName, string nextStatus, string postUrl) => $@"
<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Confirmar acción</title>
<style>*{{margin:0;padding:0;box-sizing:border-box}}body{{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:linear-gradient(120deg,#8ec5fc 0%,#e0c3fc 100%);min-height:100vh;display:flex;align-items:center;justify-content:center;padding:24px}}.card{{background:#fff;border-radius:16px;max-width:620px;width:100%;padding:36px;box-shadow:0 20px 48px rgba(0,0,0,0.18)}}h1{{font-size:28px;color:#111827;margin-bottom:12px}}p{{color:#4b5563;line-height:1.5;margin-bottom:14px}}.meta{{margin:18px 0;padding:14px;border:1px solid #e5e7eb;border-radius:10px;background:#f9fafb}}button{{width:100%;padding:12px 16px;border:0;border-radius:10px;background:#2563eb;color:#fff;font-weight:600;font-size:15px;cursor:pointer}}</style></head>
<body><div class='card'><h1>Confirmar respuesta</h1><p>Hola <strong>{clientName}</strong>, vas a cambiar el estado de la cotización <strong>{quoteNumber}</strong> a <strong>{nextStatus}</strong>.</p><p>Por seguridad, este enlace ya no actualiza el estado automáticamente. Confirma con el botón.</p><div class='meta'>Este enlace legacy está deprecado y será retirado en una próxima versión.</div><form method='post' action='{postUrl}'><input type='hidden' name='status' value='{nextStatus}' /><button type='submit'>Confirmar {nextStatus}</button></form></div></body></html>";

    private string GetAcceptedHtml(string quoteNumber, string clientName) => $@"
<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Cotización Aceptada</title>
<style>*{{margin:0;padding:0;box-sizing:border-box}}body{{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);min-height:100vh;display:flex;align-items:center;justify-content:center;padding:20px}}.container{{background:white;border-radius:20px;padding:50px 40px;max-width:600px;width:100%;box-shadow:0 20px 60px rgba(0,0,0,0.3);text-align:center}}.icon{{font-size:80px;margin-bottom:20px}}h1{{color:#10B981;font-size:32px;margin-bottom:15px}}.quote-number{{display:inline-block;background:#D1FAE5;color:#065F46;padding:10px 20px;border-radius:8px;font-weight:600;margin:20px 0;font-size:18px}}p{{color:#6B7280;font-size:16px;line-height:1.6;margin:15px 0}}.footer{{margin-top:30px;padding-top:20px;border-top:2px solid #E5E7EB;color:#9CA3AF;font-size:14px}}.company-name{{color:#6B46C1;font-weight:600}}</style></head>
<body><div class='container'><div class='icon'>✅</div><h1>¡Cotización Aceptada!</h1><p>Estimado/a <strong>{clientName}</strong>,</p><div class='quote-number'>{quoteNumber}</div><p>Hemos registrado exitosamente la <strong>aceptación</strong> de su cotización. Nuestro equipo le contactará pronto.</p><div class='footer'><p>Gracias por confiar en <span class='company-name'>FINESOFT</span><br>📞 (668) 817-1400 • ✉️ informes@finesoft.com.mx</p></div></div></body></html>";

    private string GetRejectedHtml(string quoteNumber, string clientName) => $@"
<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Cotización Rechazada</title>
<style>*{{margin:0;padding:0;box-sizing:border-box}}body{{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:linear-gradient(135deg,#f093fb 0%,#f5576c 100%);min-height:100vh;display:flex;align-items:center;justify-content:center;padding:20px}}.container{{background:white;border-radius:20px;padding:50px 40px;max-width:600px;width:100%;box-shadow:0 20px 60px rgba(0,0,0,0.3);text-align:center}}.icon{{font-size:80px;margin-bottom:20px}}h1{{color:#EF4444;font-size:32px;margin-bottom:15px}}.quote-number{{display:inline-block;background:#FEE2E2;color:#991B1B;padding:10px 20px;border-radius:8px;font-weight:600;margin:20px 0;font-size:18px}}p{{color:#6B7280;font-size:16px;line-height:1.6;margin:15px 0}}.footer{{margin-top:30px;padding-top:20px;border-top:2px solid #E5E7EB;color:#9CA3AF;font-size:14px}}.company-name{{color:#6B46C1;font-weight:600}}</style></head>
<body><div class='container'><div class='icon'>❌</div><h1>Cotización Rechazada</h1><p>Estimado/a <strong>{clientName}</strong>,</p><div class='quote-number'>{quoteNumber}</div><p>Hemos registrado que ha <strong>rechazado</strong> la cotización. Nos gustaría conocer sus comentarios para elaborar una nueva propuesta.</p><div class='footer'><p>Gracias por su tiempo.<br><span class='company-name'>FINESOFT</span> • 📞 (668) 817-1400 • ✉️ informes@finesoft.com.mx</p></div></div></body></html>";

    private string GetErrorHtml() => @"
<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><title>Error</title>
<style>*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,sans-serif;background:linear-gradient(135deg,#667eea,#764ba2);min-height:100vh;display:flex;align-items:center;justify-content:center;padding:20px}.container{background:white;border-radius:20px;padding:50px 40px;max-width:500px;width:100%;text-align:center}.icon{font-size:80px;margin-bottom:20px}h1{color:#EF4444;font-size:28px;margin-bottom:15px}p{color:#6B7280;font-size:16px;line-height:1.6}.footer{margin-top:30px;padding-top:20px;border-top:2px solid #E5E7EB;color:#9CA3AF;font-size:14px}</style></head>
<body><div class='container'><div class='icon'>⚠️</div><h1>Error al Procesar</h1><p>Lo sentimos, ocurrió un error. Por favor contacte a nuestro equipo.</p><div class='footer'><p>📞 (668) 817-1400 • ✉️ informes@finesoft.com.mx</p></div></div></body></html>";
}

public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}