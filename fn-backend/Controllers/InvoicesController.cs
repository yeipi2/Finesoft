// fs-backend/Controllers/InvoicesController.cs  — COMPLETO ACTUALIZADO
using Asp.Versioning;
using fs_backend.Attributes;
using fs_backend.DTO.Common;
using fs_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using fs_backend.Services;
using fs_backend.Util;

namespace fs_backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
        [FromQuery] PaginationQueryDto query,
        [FromQuery] string? status = null,
        [FromQuery] string? invoiceType = null,
        [FromQuery] int? clientId = null)
    {
        var invoices = await _invoiceService.GetInvoicesAsync(status, invoiceType, clientId);
        var pagedResult = ApiResponseHelper.Paginate(invoices, query, (i, search) =>
            i.InvoiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
            || i.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase)
            || i.Status.Contains(search, StringComparison.OrdinalIgnoreCase));

        return Ok(pagedResult);
    }

    /// GET: api/invoices/{id}
    [HttpGet("{id}")]
    [RequirePermission("invoices.view_detail")]
    public async Task<IActionResult> GetInvoiceById(int id)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
        if (invoice == null)
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Factura no encontrada");
        return Ok(invoice);
    }

    /// POST: api/invoices
    [HttpPost]
    [RequirePermission("invoices.create")]
    public async Task<IActionResult> CreateInvoice(InvoiceDto invoiceDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");

        var result = await _invoiceService.CreateInvoiceAsync(invoiceDto, userId);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    /// POST: api/invoices/from-quote
    [HttpPost("from-quote")]
    [RequirePermission("quotes.convert")]
    public async Task<IActionResult> CreateInvoiceFromQuote(CreateInvoiceFromQuoteDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");

        var result = await _invoiceService.CreateInvoiceFromQuoteAsync(dto, userId);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result.Data);
    }

    /// PUT: api/invoices/{id}
    [HttpPut("{id}")]
    [RequirePermission("invoices.edit")]
    public async Task<IActionResult> UpdateInvoice(int id, InvoiceDto invoiceDto)
    {
        var result = await _invoiceService.UpdateInvoiceAsync(id, invoiceDto);
        if (!result.Succeeded)
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", result.Errors.FirstOrDefault() ?? "Factura no encontrada");
        return Ok(result.Data);
    }

    /// DELETE: api/invoices/{id}
    [HttpDelete("{id}")]
    [RequirePermission("invoices.delete")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var result = await _invoiceService.DeleteInvoiceAsync(id);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);
        return NoContent();
    }

    /// PATCH: api/invoices/{id}/status
    [HttpPatch("{id}/status")]
    [RequirePermission("invoices.edit")]
    public async Task<IActionResult> ChangeInvoiceStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _invoiceService.ChangeInvoiceStatusAsync(id, request.Status, request.Reason);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);
        return Ok(new { message = "Estado actualizado exitosamente" });
    }

    /// POST: api/invoices/{id}/payments
    [HttpPost("{id}/payments")]
    [RequirePermission("invoices.payment")]
    public async Task<IActionResult> AddPayment(int id, [FromForm] RegisterInvoicePaymentDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");

        var result = await _invoiceService.AddPaymentAsync(id, dto, userId);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");

        var result = await _invoiceService.AddPaymentWithReceiptAsync(id, request, userId);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);
        return Ok(result.Data);
    }

    /// GET: api/invoices/monthly-summary
    [HttpGet("monthly-summary")]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> GetMonthlySummary()
    {
        var summary = await _invoiceService.GetMonthlySummaryAsync();
        return Ok(summary);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ⭐ ACTUALIZADO: POST api/invoices/generate-monthly
    // Recibe lista de { ClientId, PaymentMethod, PaymentForm? } por cliente
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("generate-monthly")]
    [RequirePermission("invoices.create")]
    public async Task<IActionResult> GenerateMonthlyInvoices([FromBody] GenerateMonthlyInvoicesDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");

        if (dto?.Items == null || !dto.Items.Any())
            return this.ToValidationProblem(new[] { "Debes seleccionar al menos un cliente." });

        // Validar que todos los items tengan PaymentMethod
        var sinMetodo = dto.Items.Where(i => string.IsNullOrEmpty(i.PaymentMethod)).ToList();
        if (sinMetodo.Any())
            return this.ToValidationProblem(new[] { $"Faltan métodos de pago para {sinMetodo.Count} cliente(s)." });

        // Validar que PUE tenga PaymentForm
        var puesinforma = dto.Items
            .Where(i => i.PaymentMethod == "PUE" && string.IsNullOrEmpty(i.PaymentForm))
            .ToList();
        if (puesinforma.Any())
            return this.ToValidationProblem(new[] { $"{puesinforma.Count} cliente(s) con PUE no tienen forma de pago." });

        _logger.LogInformation("Usuario {UserId} generando {Count} facturas mensuales", userId, dto.Items.Count);

        var result = await _invoiceService.GenerateMonthlyInvoicesAsync(userId, dto.Items);
        if (!result.Succeeded)
            return this.ToValidationProblem(result.Errors);

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
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", ex.Message);
        }
    }
}
