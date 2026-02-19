using fs_backend.Attributes;
using fs_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// GET: api/invoices
    /// Requiere permiso: invoices.view
    /// </summary>
    [HttpGet]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? status = null,
        [FromQuery] string? invoiceType = null,
        [FromQuery] int? clientId = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo facturas", userId);

        var invoices = await _invoiceService.GetInvoicesAsync(status, invoiceType, clientId);
        return Ok(invoices);
    }

    /// <summary>
    /// GET: api/invoices/{id}
    /// Requiere permiso: invoices.view_detail
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("invoices.view_detail")]
    public async Task<IActionResult> GetInvoiceById(int id)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
        if (invoice == null)
        {
            return NotFound(new { message = "Factura no encontrada" });
        }

        return Ok(invoice);
    }

    /// <summary>
    /// POST: api/invoices
    /// Requiere permiso: invoices.create
    /// </summary>
    [HttpPost]
    [RequirePermission("invoices.create")]
    public async Task<IActionResult> CreateInvoice(InvoiceDto invoiceDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        _logger.LogInformation("✅ Usuario {UserId} creando factura", userId);

        var result = await _invoiceService.CreateInvoiceAsync(invoiceDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// POST: api/invoices/from-quote
    /// Requiere permisos: quotes.convert + invoices.create
    /// </summary>
    [HttpPost("from-quote")]
    [RequirePermission("quotes.convert")]
    public async Task<IActionResult> CreateInvoiceFromQuote(CreateInvoiceFromQuoteDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        _logger.LogInformation("✅ Usuario {UserId} convirtiendo cotización a factura", userId);

        var result = await _invoiceService.CreateInvoiceFromQuoteAsync(dto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/invoices/{id}
    /// Requiere permiso: invoices.edit
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("invoices.edit")]
    public async Task<IActionResult> UpdateInvoice(int id, InvoiceDto invoiceDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} actualizando factura {InvoiceId}", userId, id);

        var result = await _invoiceService.UpdateInvoiceAsync(id, invoiceDto);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// DELETE: api/invoices/{id}
    /// Requiere permiso: invoices.delete
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("invoices.delete")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} eliminando factura {InvoiceId}", userId, id);

        var result = await _invoiceService.DeleteInvoiceAsync(id);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Factura eliminada exitosamente" });
    }

    /// <summary>
    /// PATCH: api/invoices/{id}/status
    /// Requiere permiso: invoices.edit
    /// </summary>
    [HttpPatch("{id}/status")]
    [RequirePermission("invoices.edit")]
    public async Task<IActionResult> ChangeInvoiceStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _invoiceService.ChangeInvoiceStatusAsync(id, request.Status, request.Reason);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Estado actualizado exitosamente" });
    }


    /// <summary>
    /// POST: api/invoices/{id}/payments
    /// Requiere permiso: invoices.payment
    /// </summary>
    [HttpPost("{id}/payments")]
    [RequirePermission("invoices.payment")]
    public async Task<IActionResult> AddPayment(int id, InvoicePaymentDto paymentDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        _logger.LogInformation("✅ Usuario {UserId} registrando pago para factura {InvoiceId}", userId, id);

        var result = await _invoiceService.AddPaymentAsync(id, paymentDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// POST: api/invoices/generate-monthly
    /// Requiere permiso: invoices.create
    /// </summary>
    [HttpPost("generate-monthly")]
    [RequirePermission("invoices.create")]
    public async Task<IActionResult> GenerateMonthlyInvoices()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        _logger.LogInformation("✅ Usuario {UserId} generando facturas mensuales", userId);

        var result = await _invoiceService.GenerateMonthlyInvoicesAsync(userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Facturas mensuales generadas exitosamente" });
    }

    /// <summary>
    /// GET: api/invoices/stats
    /// Requiere permiso: invoices.view
    /// </summary>
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

    /// <summary>
    /// GET: api/invoices/{id}/pdf
    /// Requiere permiso: invoices.view_detail
    /// </summary>
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