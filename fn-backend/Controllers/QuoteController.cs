using fs_backend.Attributes;
using fs_backend.DTO;
using fs_backend.Identity;
using fs_backend.Repositories;
using fs_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public QuotesController(
        IQuoteService quoteService,
        ILogger<QuotesController> logger,
        ApplicationDbContext context)
    {
        _quoteService = quoteService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// GET: api/quotes
    /// Requiere permiso: quotes.view
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
    /// Requiere permiso: quotes.view_detail
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("quotes.view_detail")]
    public async Task<IActionResult> GetQuoteById(int id)
    {
        var quote = await _quoteService.GetQuoteByIdAsync(id);
        if (quote == null)
        {
            return NotFound(new { message = "Cotización no encontrada" });
        }

        return Ok(quote);
    }

    /// <summary>
    /// POST: api/quotes
    /// Requiere permiso: quotes.create
    /// </summary>
    [HttpPost]
    [RequirePermission("quotes.create")]
    public async Task<IActionResult> CreateQuote(QuoteDto quoteDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        _logger.LogInformation("✅ Usuario {UserId} creando cotización", userId);

        var result = await _quoteService.CreateQuoteAsync(quoteDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetQuoteById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/quotes/{id}
    /// Requiere permiso: quotes.edit
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("quotes.edit")]
    public async Task<IActionResult> UpdateQuote(int id, QuoteDto quoteDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} actualizando cotización {QuoteId}", userId, id);

        var result = await _quoteService.UpdateQuoteAsync(id, quoteDto);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// DELETE: api/quotes/{id}
    /// Requiere permiso: quotes.delete
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("quotes.delete")]
    public async Task<IActionResult> DeleteQuote(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} eliminando cotización {QuoteId}", userId, id);

        var result = await _quoteService.DeleteQuoteAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Cotización eliminada exitosamente" });
    }

    /// <summary>
    /// PATCH: api/quotes/{id}/status
    /// Requiere permiso: quotes.edit
    /// </summary>
    [HttpPatch("{id}/status")]
    [RequirePermission("quotes.edit")]
    public async Task<IActionResult> ChangeQuoteStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _quoteService.ChangeQuoteStatusAsync(id, request.Status);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Estado actualizado exitosamente" });
    }

    /// <summary>
    /// POST: api/quotes/{id}/send-email
    /// Envía la cotización por correo electrónico al cliente y cambia el estado a "Enviada"
    /// Requiere permiso: quotes.edit
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
            {
                return NotFound(new { message = "Cotización no encontrada" });
            }

            if (string.IsNullOrEmpty(quote.Client?.Email))
            {
                return BadRequest(new { message = "El cliente no tiene email registrado" });
            }

            // Generar PDF
            var pdfBytes = await _quoteService.GenerateQuotePdfAsync(id);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return BadRequest(new { message = "No se pudo generar el PDF" });
            }

            // Enviar email
            var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
            var emailSent = await emailService.SendQuoteEmailAsync(
                quote.Client.Email,
                quote.Client.CompanyName,
                quote.QuoteNumber,
                pdfBytes
            );

            if (!emailSent)
            {
                return StatusCode(500, new { message = "Error al enviar el correo electrónico" });
            }

            // Cambiar estado a "Enviada" automáticamente
            quote.Status = "Enviada";
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Email enviado y estado actualizado para cotización {QuoteId}", id);

            return Ok(new
            {
                message = "Cotización enviada exitosamente por correo electrónico",
                newStatus = "Enviada"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email para cotización {QuoteId}", id);
            return StatusCode(500, new { message = "Error al enviar la cotización" });
        }
    }

    /// <summary>
    /// GET: api/quotes/{id}/pdf
    /// Requiere permiso: quotes.view_detail
    /// </summary>
    [HttpGet("{id}/pdf")]
    [RequirePermission("quotes.view_detail")]
    public async Task<IActionResult> GetQuotePdf(int id)
    {
        try
        {
            _logger.LogInformation("📥 Generando PDF para cotización {QuoteId}", id);

            var pdfBytes = await _quoteService.GenerateQuotePdfAsync(id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return BadRequest(new { message = "No se pudo generar el PDF" });
            }

            _logger.LogInformation("✅ PDF generado: {Size} bytes", pdfBytes.Length);

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

    // ⭐ NUEVO: Endpoint público para que el cliente acepte desde el email
    /// <summary>
    /// GET: api/quotes/accept/{quoteNumber}
    /// Permite al cliente aceptar una cotización desde el link del email (sin autenticación)
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
            {
                return NotFound(new { message = "Cotización no encontrada" });
            }

            // Solo se puede aceptar si está en estado "Enviada"
            if (quote.Status != "Enviada")
            {
                return BadRequest(new
                {
                    message = $"Esta cotización ya tiene el estado '{quote.Status}' y no puede ser modificada.",
                    currentStatus = quote.Status
                });
            }

            quote.Status = "Aceptada";
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Cotización {QuoteNumber} aceptada por el cliente desde email", quoteNumber);

            // Retornar HTML de confirmación
            return Content(GetAcceptedHtml(quote.QuoteNumber, quote.Client?.CompanyName ?? "Cliente"), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al aceptar cotización {QuoteNumber}", quoteNumber);
            return Content(GetErrorHtml(), "text/html");
        }
    }

    // ⭐ NUEVO: Endpoint público para que el cliente rechace desde el email
    /// <summary>
    /// GET: api/quotes/reject/{quoteNumber}
    /// Permite al cliente rechazar una cotización desde el link del email (sin autenticación)
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
            {
                return NotFound(new { message = "Cotización no encontrada" });
            }

            // Solo se puede rechazar si está en estado "Enviada"
            if (quote.Status != "Enviada")
            {
                return BadRequest(new
                {
                    message = $"Esta cotización ya tiene el estado '{quote.Status}' y no puede ser modificada.",
                    currentStatus = quote.Status
                });
            }

            quote.Status = "Rechazada";
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();

            _logger.LogInformation("❌ Cotización {QuoteNumber} rechazada por el cliente desde email", quoteNumber);

            // Retornar HTML de confirmación
            return Content(GetRejectedHtml(quote.QuoteNumber, quote.Client?.CompanyName ?? "Cliente"), "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al rechazar cotización {QuoteNumber}", quoteNumber);
            return Content(GetErrorHtml(), "text/html");
        }
    }

    // 🎨 HTML para página de confirmación de aceptación
    private string GetAcceptedHtml(string quoteNumber, string clientName)
    {
        return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Cotización Aceptada</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        
        .container {{
            background: white;
            border-radius: 20px;
            padding: 50px 40px;
            max-width: 600px;
            width: 100%;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
        }}
        
        .icon {{
            font-size: 80px;
            margin-bottom: 20px;
            animation: scaleIn 0.5s ease-out;
        }}
        
        @keyframes scaleIn {{
            from {{
                transform: scale(0);
                opacity: 0;
            }}
            to {{
                transform: scale(1);
                opacity: 1;
            }}
        }}
        
        h1 {{
            color: #10B981;
            font-size: 32px;
            margin-bottom: 15px;
        }}
        
        .quote-number {{
            display: inline-block;
            background: #D1FAE5;
            color: #065F46;
            padding: 10px 20px;
            border-radius: 8px;
            font-weight: 600;
            margin: 20px 0;
            font-size: 18px;
        }}
        
        p {{
            color: #6B7280;
            font-size: 16px;
            line-height: 1.6;
            margin: 15px 0;
        }}
        
        .message-box {{
            background: #F9FAFB;
            border-radius: 12px;
            padding: 20px;
            margin: 25px 0;
            text-align: left;
        }}
        
        .message-box h3 {{
            color: #374151;
            font-size: 16px;
            margin-bottom: 10px;
        }}
        
        .message-box ul {{
            list-style: none;
            padding: 0;
        }}
        
        .message-box li {{
            padding: 8px 0;
            color: #4B5563;
        }}
        
        .message-box li::before {{
            content: '✓';
            color: #10B981;
            font-weight: bold;
            margin-right: 10px;
        }}
        
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 2px solid #E5E7EB;
            color: #9CA3AF;
            font-size: 14px;
        }}
        
        .company-name {{
            color: #6B46C1;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>✅</div>
        <h1>¡Cotización Aceptada!</h1>
        
        <p>Estimado/a <strong>{clientName}</strong>,</p>
        
        <div class='quote-number'>{quoteNumber}</div>
        
        <p>
            Hemos registrado exitosamente la <strong>aceptación</strong> de su cotización.
            Nuestro equipo ha sido notificado y procederá con los siguientes pasos.
        </p>
        
        <div class='message-box'>
            <h3>📋 Próximos Pasos:</h3>
            <ul>
                <li>Nuestro equipo revisará su aceptación</li>
                <li>Le contactaremos para coordinar los detalles</li>
                <li>Recibirá la factura correspondiente</li>
                <li>Iniciaremos el proceso de implementación</li>
            </ul>
        </div>
        
        <p>
            Si tiene alguna pregunta o necesita información adicional, 
            no dude en contactarnos.
        </p>
        
        <div class='footer'>
            <p>
                Gracias por confiar en <span class='company-name'>FINESOFT</span><br>
                📞 (668) 817-1400 • ✉️ informes@finesoft.com.mx
            </p>
        </div>
    </div>
</body>
</html>";
    }

    // 🎨 HTML para página de confirmación de rechazo
    private string GetRejectedHtml(string quoteNumber, string clientName)
    {
        return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Cotización Rechazada</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        
        .container {{
            background: white;
            border-radius: 20px;
            padding: 50px 40px;
            max-width: 600px;
            width: 100%;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
        }}
        
        .icon {{
            font-size: 80px;
            margin-bottom: 20px;
            animation: scaleIn 0.5s ease-out;
        }}
        
        @keyframes scaleIn {{
            from {{
                transform: scale(0);
                opacity: 0;
            }}
            to {{
                transform: scale(1);
                opacity: 1;
            }}
        }}
        
        h1 {{
            color: #EF4444;
            font-size: 32px;
            margin-bottom: 15px;
        }}
        
        .quote-number {{
            display: inline-block;
            background: #FEE2E2;
            color: #991B1B;
            padding: 10px 20px;
            border-radius: 8px;
            font-weight: 600;
            margin: 20px 0;
            font-size: 18px;
        }}
        
        p {{
            color: #6B7280;
            font-size: 16px;
            line-height: 1.6;
            margin: 15px 0;
        }}
        
        .message-box {{
            background: #FEF3C7;
            border-radius: 12px;
            padding: 20px;
            margin: 25px 0;
            text-align: left;
            border-left: 4px solid #F59E0B;
        }}
        
        .message-box p {{
            margin: 0;
            color: #92400E;
        }}
        
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 2px solid #E5E7EB;
            color: #9CA3AF;
            font-size: 14px;
        }}
        
        .company-name {{
            color: #6B46C1;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>❌</div>
        <h1>Cotización Rechazada</h1>
        
        <p>Estimado/a <strong>{clientName}</strong>,</p>
        
        <div class='quote-number'>{quoteNumber}</div>
        
        <p>
            Hemos registrado que ha <strong>rechazado</strong> la cotización.
            Lamentamos que nuestra propuesta no haya cumplido con sus expectativas.
        </p>
        
        <div class='message-box'>
            <p>
                <strong>💡 ¿Podemos mejorar?</strong><br>
                Nos gustaría conocer sus comentarios para elaborar una nueva propuesta 
                que se ajuste mejor a sus necesidades. No dude en contactarnos.
            </p>
        </div>
        
        <p>
            Nuestro equipo está disponible para discutir alternativas y 
            encontrar la mejor solución para su proyecto.
        </p>
        
        <div class='footer'>
            <p>
                Gracias por su tiempo. Esperamos poder servirle en el futuro.<br>
                <span class='company-name'>FINESOFT</span><br>
                📞 (668) 817-1400 • ✉️ informes@finesoft.com.mx
            </p>
        </div>
    </div>
</body>
</html>";
    }

    // 🎨 HTML para página de error
    private string GetErrorHtml()
    {
        return @"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Error</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        
        .container {
            background: white;
            border-radius: 20px;
            padding: 50px 40px;
            max-width: 500px;
            width: 100%;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
        }
        
        .icon {
            font-size: 80px;
            margin-bottom: 20px;
        }
        
        h1 {
            color: #EF4444;
            font-size: 28px;
            margin-bottom: 15px;
        }
        
        p {
            color: #6B7280;
            font-size: 16px;
            line-height: 1.6;
        }
        
        .footer {
            margin-top: 30px;
            padding-top: 20px;
            border-top: 2px solid #E5E7EB;
            color: #9CA3AF;
            font-size: 14px;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>⚠️</div>
        <h1>Error al Procesar</h1>
        <p>
            Lo sentimos, ocurrió un error al procesar su solicitud. 
            Por favor, intente nuevamente más tarde o contacte a nuestro equipo de soporte.
        </p>
        <div class='footer'>
            <p>
                📞 (668) 817-1400 • ✉️ informes@finesoft.com.mx
            </p>
        </div>
    </div>
</body>
</html>";
    }
}

public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty;
}