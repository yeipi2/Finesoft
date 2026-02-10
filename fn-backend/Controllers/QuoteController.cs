using fs_backend.DTO;
using fs_backend.Repositories;
using fs_backend.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(IQuoteService quoteService, ILogger<QuotesController> logger)
    {
        _quoteService = quoteService;
        _logger = logger;
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
    /// GET: api/quotes/{id}/pdf
    /// Requiere permiso: quotes.view_detail
    /// </summary>
    [HttpGet("{id}/pdf")]
    [RequirePermission("quotes.view_detail")]
    public async Task<IActionResult> GetQuotePdf(int id)
    {
        try
        {
            var pdfBytes = await _quoteService.GenerateQuotePdfAsync(id);
            return File(pdfBytes, "text/html", $"Cotizacion-{id}.html");
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty;
}