using fs_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]


public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;

    public QuotesController(IQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuotes([FromQuery] string? status = null, [FromQuery] int? clientId = null)
    {
        var quotes = await _quoteService.GetQuotesAsync(status, clientId);
        return Ok(quotes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuoteById(int id)
    {
        var quote = await _quoteService.GetQuoteByIdAsync(id);
        if (quote == null)
        {
            return NotFound(new { message = "Cotización no encontrada" });
        }

        return Ok(quote);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuote(QuoteDto quoteDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _quoteService.CreateQuoteAsync(quoteDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetQuoteById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuote(int id, QuoteDto quoteDto)
    {
        var result = await _quoteService.UpdateQuoteAsync(id, quoteDto);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuote(int id)
    {
        var result = await _quoteService.DeleteQuoteAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Cotización eliminada exitosamente" });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeQuoteStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _quoteService.ChangeQuoteStatusAsync(id, request.Status);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Estado actualizado exitosamente" });
    }

    [HttpGet("{id}/pdf")]
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