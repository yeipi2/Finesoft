using fn_backend.DTO;
using fs_backend.Attributes;
using fs_backend.DTO;
using fs_backend.Hubs;
using fs_backend.Identity;
using fs_backend.Repositories;
using fs_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ILogger<QuotesController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<QuotesHub> _quotesHub; // ⭐ NUEVO

    public QuotesController(
        IQuoteService quoteService,
        ILogger<QuotesController> logger,
        ApplicationDbContext context,
        IHubContext<QuotesHub> quotesHub) // ⭐ NUEVO
    {
        _quoteService = quoteService;
        _logger = logger;
        _context = context;
        _quotesHub = quotesHub; // ⭐ NUEVO
    }

    /// <summary>
    /// GET: api/quotes
    /// </summary>
    [HttpGet]
    [RequirePermission("quotes.view")]
    public async Task<IActionResult> GetQuotes(
        [FromQuery] string? status = null,
        [FromQuery] int? clientId = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo cotizaciones", userId);
        var quotes = await _quoteService.GetQuotesAsync(status, clientId);
        return Ok(quotes);
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
            return NotFound(new { message = "Cotización no encontrada" });
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
            return Unauthorized(new { message = "Usuario no autenticado" });

        _logger.LogInformation("✅ Usuario {UserId} creando cotización", userId);
        var result = await _quoteService.CreateQuoteAsync(quoteDto, userId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

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
            return NotFound(result.Errors);
        return Ok(result.Data);
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
            return NotFound(result.Errors);
        return Ok(new { message = "Cotización eliminada exitosamente" });
    }

    /// <summary>
    /// PATCH: api/quotes/{id}/status
    /// </summary>
    [HttpPatch("{id}/status")]
    [RequirePermission("quotes.edit")]
    public async Task<IActionResult> ChangeQuoteStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _quoteService.ChangeQuoteStatusAsync(id, request.Status);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(new { message = "Estado actualizado exitosamente" });
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

            var quote = await _context.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
                return NotFound(new { message = "Cotización no encontrada" });

            if (string.IsNullOrEmpty(quote.Client?.Email))
                return BadRequest(new { message = "El cliente no tiene email registrado" });

            var pdfBytes = await _quoteService.GenerateQuotePdfAsync(id);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return BadRequest(new { message = "No se pudo generar el PDF" });

            if (string.IsNullOrEmpty(quote.PublicToken))
            {
                quote.PublicToken = Guid.NewGuid().ToString("N");
                _context.Quotes.Update(quote);
                await _context.SaveChangesAsync();
            }

            var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
            var emailSent = await emailService.SendQuoteEmailAsync(
                quote.Client.Email,
                quote.Client.CompanyName,
                quote.QuoteNumber,
                pdfBytes,
                quote.PublicToken
            );

            if (!emailSent)
                return StatusCode(500, new { message = "Error al enviar el correo electrónico" });

            quote.Status = "Enviada";
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Email enviado y estado actualizado para cotización {QuoteId}", id);

            return Ok(new { message = "Cotización enviada exitosamente por correo electrónico", newStatus = "Enviada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email para cotización {QuoteId}", id);
            return StatusCode(500, new { message = "Error al enviar la cotización" });
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
                return BadRequest(new { message = "No se pudo generar el PDF" });

            var cd = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline")
            {
                FileName = $"Cotizacion-{id}.pdf"
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generando PDF para cotización {QuoteId}", id);
            return StatusCode(500, new { message = "Error generando PDF" });
        }
    }

    /// <summary>
    /// GET: api/quotes/public/{token}
    /// ⭐ PÚBLICO
    /// </summary>
    [HttpGet("public/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQuoteByPublicToken(string token)
    {
        try
        {
            var quote = await _quoteService.GetQuoteByPublicTokenAsync(token);
            if (quote == null)
                return NotFound(new { message = "Cotización no encontrada o enlace inválido" });
            return Ok(quote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cotización por token público");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/quotes/public/{token}/respond
    /// ⭐ PÚBLICO - Acepta o rechaza la cotización y notifica via SignalR
    /// </summary>
    [HttpPost("public/{token}/respond")]
    [AllowAnonymous]
    public async Task<IActionResult> RespondToQuote(string token, [FromBody] RespondQuoteDto dto)
    {
        try
        {
            // 1. Obtener info de la cotización ANTES de responder (para tener el ID y número)
            var quoteInfo = await _quoteService.GetQuoteByPublicTokenAsync(token);
            if (quoteInfo == null)
                return NotFound(new { message = "Cotización no encontrada" });

            // 2. Guardar la respuesta
            var result = await _quoteService.RespondToQuoteAsync(token, dto.Status, dto.Comments);
            if (!result.Succeeded)
                return BadRequest(new { message = result.Errors.FirstOrDefault() ?? "Error al responder" });

            // 3. ⭐ Notificar a todos los usuarios internos conectados via SignalR
            await _quotesHub.Clients.Group("internal-users").SendAsync("QuoteStatusChanged", new
            {
                quoteId = quoteInfo.Id,
                quoteNumber = quoteInfo.QuoteNumber,
                clientName = quoteInfo.ClientName,
                newStatus = dto.Status
            });

            _logger.LogInformation("📡 SignalR: cotización {QuoteNumber} → {Status}", quoteInfo.QuoteNumber, dto.Status);

            return Ok(new { message = "Respuesta registrada correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al responder cotización");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET: api/quotes/accept/{quoteNumber}
    /// </summary>
    [HttpGet("accept/{quoteNumber}")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptQuoteFromEmail(string quoteNumber)
    {
        try
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.QuoteNumber == quoteNumber);

            if (quote == null)
                return NotFound(new { message = "Cotización no encontrada" });

            if (quote.Status != "Enviada")
                return BadRequest(new { message = $"Esta cotización ya tiene el estado '{quote.Status}'.", currentStatus = quote.Status });

            quote.Status = "Aceptada";
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();

            // ⭐ Notificar via SignalR también desde este endpoint legacy
            await _quotesHub.Clients.Group("internal-users").SendAsync("QuoteStatusChanged", new
            {
                quoteId = quote.Id,
                quoteNumber = quote.QuoteNumber,
                clientName = quote.Client?.CompanyName ?? "",
                newStatus = "Aceptada"
            });

            return Content(GetAcceptedHtml(quote.QuoteNumber, quote.Client?.CompanyName ?? "Cliente"), "text/html");
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
    [HttpGet("reject/{quoteNumber}")]
    [AllowAnonymous]
    public async Task<IActionResult> RejectQuoteFromEmail(string quoteNumber)
    {
        try
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.QuoteNumber == quoteNumber);

            if (quote == null)
                return NotFound(new { message = "Cotización no encontrada" });

            if (quote.Status != "Enviada")
                return BadRequest(new { message = $"Esta cotización ya tiene el estado '{quote.Status}'.", currentStatus = quote.Status });

            quote.Status = "Rechazada";
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();

            // ⭐ Notificar via SignalR también desde este endpoint legacy
            await _quotesHub.Clients.Group("internal-users").SendAsync("QuoteStatusChanged", new
            {
                quoteId = quote.Id,
                quoteNumber = quote.QuoteNumber,
                clientName = quote.Client?.CompanyName ?? "",
                newStatus = "Rechazada"
            });

            return Content(GetRejectedHtml(quote.QuoteNumber, quote.Client?.CompanyName ?? "Cliente"), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al rechazar cotización {QuoteNumber}", quoteNumber);
            return Content(GetErrorHtml(), "text/html");
        }
    }

    // 🎨 HTML helpers (sin cambios)
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