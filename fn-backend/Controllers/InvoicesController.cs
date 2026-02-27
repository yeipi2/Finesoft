// fs-backend/Controllers/InvoicesController.cs  — COMPLETO ACTUALIZADO
using fs_backend.Attributes;
using fs_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using fs_backend.Services;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// GET: api/invoices
    [HttpGet]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? status = null,
        [FromQuery] string? invoiceType = null,
        [FromQuery] int? clientId = null)
    {
        var invoices = await _invoiceService.GetInvoicesAsync(status, invoiceType, clientId);
        return Ok(invoices);
    }

    /// GET: api/invoices/{id}
    [HttpGet("{id}")]
    [RequirePermission("invoices.view_detail")]
    public async Task<IActionResult> GetInvoiceById(int id)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
        if (invoice == null)
            return NotFound(new { message = "Factura no encontrada" });
        return Ok(invoice);
    }

    /// POST: api/invoices
    [HttpPost]
    [RequirePermission("invoices.create")]
    public async Task<IActionResult> CreateInvoice(InvoiceDto invoiceDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuario no autenticado" });

        var result = await _invoiceService.CreateInvoiceAsync(invoiceDto, userId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    /// POST: api/invoices/from-quote
    [HttpPost("from-quote")]
    [RequirePermission("quotes.convert")]
    public async Task<IActionResult> CreateInvoiceFromQuote(CreateInvoiceFromQuoteDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuario no autenticado" });

        var result = await _invoiceService.CreateInvoiceFromQuoteAsync(dto, userId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    /// PUT: api/invoices/{id}
    [HttpPut("{id}")]
    [RequirePermission("invoices.edit")]
    public async Task<IActionResult> UpdateInvoice(int id, InvoiceDto invoiceDto)
    {
        var result = await _invoiceService.UpdateInvoiceAsync(id, invoiceDto);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result.Data);
    }

    /// DELETE: api/invoices/{id}
    [HttpDelete("{id}")]
    [RequirePermission("invoices.delete")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var result = await _invoiceService.DeleteInvoiceAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(new { message = "Factura eliminada exitosamente" });
    }

    /// PATCH: api/invoices/{id}/status
    [HttpPatch("{id}/status")]
    [RequirePermission("invoices.edit")]
    public async Task<IActionResult> ChangeInvoiceStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _invoiceService.ChangeInvoiceStatusAsync(id, request.Status, request.Reason);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(new { message = "Estado actualizado exitosamente" });
    }

    /// POST: api/invoices/{id}/payments
    [HttpPost("{id}/payments")]
    [RequirePermission("invoices.payment")]
    public async Task<IActionResult> AddPayment(int id, [FromForm] RegisterInvoicePaymentDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuario no autenticado" });

        var result = await _invoiceService.AddPaymentAsync(id, dto, userId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result.Data);
    }

    /// POST: api/invoices/{id}/payments-with-receipt
    [HttpPost("{id}/payments-with-receipt")]
    [RequirePermission("invoices.payment")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> AddPaymentWithReceipt(int id, [FromForm] AddInvoicePaymentWithReceiptRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuario no autenticado" });

        var result = await _invoiceService.AddPaymentWithReceiptAsync(id, request, userId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result.Data);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ⭐ NUEVO: GET api/invoices/monthly-summary
    // Devuelve el resumen de pólizas mensuales con tickets y estado para el panel
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("monthly-summary")]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> GetMonthlySummary()
    {
        var summary = await _invoiceService.GetMonthlySummaryAsync();
        return Ok(summary);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ⭐ ACTUALIZADO: POST api/invoices/generate-monthly
    // Acepta lista de clientIds seleccionados. Si está vacía, genera todos.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("generate-monthly")]
    [RequirePermission("invoices.create")]
    public async Task<IActionResult> GenerateMonthlyInvoices([FromBody] GenerateMonthlyInvoicesDto? dto = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuario no autenticado" });

        _logger.LogInformation("✅ Usuario {UserId} generando facturas mensuales para {Count} clientes",
            userId, dto?.ClientIds?.Count ?? 0);

        var result = await _invoiceService.GenerateMonthlyInvoicesAsync(userId, dto?.ClientIds);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Facturas mensuales generadas exitosamente" });
    }

    /// GET: api/invoices/stats
    [HttpGet("stats")]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> GetInvoiceStats()
    {
        var stats = await _invoiceService.GetInvoiceStatsAsync();
        return Ok(stats);
    }

    [HttpGet("tickets-in-use")]
    [Authorize]
    public async Task<IActionResult> GetTicketsInUse()
    {
        var ticketIds = await _invoiceService.GetTicketsInUseAsync();
        return Ok(ticketIds);
    }

    /// GET: api/invoices/{id}/pdf
    [HttpGet("{id}/pdf")]
    [RequirePermission("invoices.view_detail")]
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