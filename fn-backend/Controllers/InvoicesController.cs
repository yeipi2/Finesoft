using fs_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? status = null,
        [FromQuery] string? invoiceType = null,
        [FromQuery] int? clientId = null)
    {
        var invoices = await _invoiceService.GetInvoicesAsync(status, invoiceType, clientId);
        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoiceById(int id)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
        if (invoice == null)
        {
            return NotFound(new { message = "Factura no encontrada" });
        }

        return Ok(invoice);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice(InvoiceDto invoiceDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _invoiceService.CreateInvoiceAsync(invoiceDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPost("from-quote")]
    public async Task<IActionResult> CreateInvoiceFromQuote(CreateInvoiceFromQuoteDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _invoiceService.CreateInvoiceFromQuoteAsync(dto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInvoice(int id, InvoiceDto invoiceDto)
    {
        var result = await _invoiceService.UpdateInvoiceAsync(id, invoiceDto);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var result = await _invoiceService.DeleteInvoiceAsync(id);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Factura eliminada exitosamente" });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeInvoiceStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _invoiceService.ChangeInvoiceStatusAsync(id, request.Status);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Estado actualizado exitosamente" });
    }

    [HttpPost("{id}/payments")]
    public async Task<IActionResult> AddPayment(int id, InvoicePaymentDto paymentDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _invoiceService.AddPaymentAsync(id, paymentDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("generate-monthly")]
    public async Task<IActionResult> GenerateMonthlyInvoices()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _invoiceService.GenerateMonthlyInvoicesAsync(userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Facturas mensuales generadas exitosamente" });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetInvoiceStats()
    {
        var stats = await _invoiceService.GetInvoiceStatsAsync();
        return Ok(stats);
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id)
    {
        try
        {
            var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(id);
            return File(pdfBytes, "application/pdf", $"Factura-{id}.pdf");
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}