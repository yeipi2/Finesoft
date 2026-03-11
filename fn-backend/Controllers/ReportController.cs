using Asp.Versioning;
using fs_backend.Services;
using fs_backend.Attributes;
using fs_backend.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportPdfGenerator _pdfGenerator;
    private readonly IReportEmailPreferenceService _emailPreferenceService;
    private readonly IEmailService _emailService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly Identity.ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        IReportPdfGenerator pdfGenerator,
        IReportEmailPreferenceService emailPreferenceService,
        IEmailService emailService,
        UserManager<IdentityUser> userManager,
        Identity.ApplicationDbContext context,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _pdfGenerator = pdfGenerator;
        _emailPreferenceService = emailPreferenceService;
        _emailService = emailService;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/reports/dashboard
    /// Requiere permiso: dashboard.view
    /// </summary>
    [HttpGet("dashboard")]
    [RequirePermission("dashboard.view")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo stats del dashboard", userId);

        var stats = await _reportService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// GET: api/reports/by-user
    /// Requiere permiso: reports.view
    /// </summary>
    [HttpGet("by-user")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetReportsByUser(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo reportes por usuario", userId);

        var reports = await _reportService.GetReportsByUserAsync(startDate, endDate);
        return Ok(reports);
    }

    /// <summary>
    /// GET: api/reports/by-client
    /// Requiere permiso: reports.view
    /// </summary>
    [HttpGet("by-client")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetReportsByClient(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var reports = await _reportService.GetReportsByClientAsync(startDate, endDate);
        return Ok(reports);
    }

    /// <summary>
    /// GET: api/reports/by-project
    /// Requiere permiso: reports.view
    /// </summary>
    [HttpGet("by-project")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetReportsByProject(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var reports = await _reportService.GetReportsByProjectAsync(startDate, endDate);
        return Ok(reports);
    }

    /// <summary>
    /// GET: api/reports/financial
    /// ✅ FIX: Cambiado de "reports.financial" a "reports.view"
    /// El Supervisor tiene reports.view y debe poder ver datos financieros
    /// </summary>
    [HttpGet("financial")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetFinancialReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo reporte financiero", userId);

        var report = await _reportService.GetFinancialReportAsync(startDate, endDate);
        return Ok(report);
    }

    /// <summary>
    /// GET: api/reports/performance
    /// Requiere permiso: reports.view
    /// </summary>
    [HttpGet("performance")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetPerformanceMetrics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var metrics = await _reportService.GetPerformanceMetricsAsync(startDate, endDate);
        return Ok(metrics);
    }

    /// <summary>
    /// GET: api/reports/revenue-trend
    /// ✅ FIX: Cambiado de "reports.financial" a "reports.view"
    /// La tendencia de ingresos es un dato visual, no requiere permiso especial
    /// </summary>
    [HttpGet("revenue-trend")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] int months = 12)
    {
        var trend = await _reportService.GetRevenueTrendAsync(months);
        return Ok(trend);
    }

    /// <summary>
    /// GET: api/reports/tickets-by-status
    /// Requiere permiso: reports.view
    /// </summary>
    [HttpGet("tickets-by-status")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetTicketsByStatus()
    {
        var data = await _reportService.GetTicketsByStatusAsync();
        return Ok(data);
    }

    /// <summary>
    /// GET: api/reports/public-stats
    /// Endpoint público para la landing page
    /// </summary>
    [HttpGet("public-stats")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicApi")]
    public async Task<IActionResult> GetPublicStats()
    {
        var stats = await _reportService.GetPublicStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// GET: api/reports/top-clients
    /// Requiere permiso: reports.view
    /// </summary>
    [HttpGet("top-clients")]
    [RequirePermission("reports.view")]
    public async Task<IActionResult> GetTopClients([FromQuery] int top = 10)
    {
        var clients = await _reportService.GetTopClientsAsync(top);
        return Ok(clients);
    }

    /// <summary>
    /// GET: api/reports/email-preference
    /// Requiere permiso: reports.config_email
    /// </summary>
    [HttpGet("email-preference")]
    [RequirePermission("reports.config_email")]
    public async Task<IActionResult> GetEmailPreference()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var pref = await _emailPreferenceService.GetPreferenceAsync(userId);

        if (pref == null)
            return Ok(new ReportEmailPreferenceDto
            {
                UserId = userId,
                AutoSendEnabled = false,
                Frequency = "weekly"
            });

        return Ok(pref);
    }

    /// <summary>
    /// PUT: api/reports/email-preference
    /// Requiere permiso: reports.config_email
    /// </summary>
    [HttpPut("email-preference")]
    [RequirePermission("reports.config_email")]
    public async Task<IActionResult> UpdateEmailPreference([FromBody] UpdateReportEmailPreferenceDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var pref = await _emailPreferenceService.UpdatePreferenceAsync(userId, dto);
        return Ok(pref);
    }

    /// <summary>
    /// POST: api/reports/export-pdf
    /// ✅ Solo requiere reports.export — botón solo visible si tiene este permiso
    /// </summary>
    [HttpPost("export-pdf")]
    [RequirePermission("reports.export")]
    public async Task<IActionResult> ExportPdf([FromBody] SendReportEmailRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("📄 Usuario {UserId} exportando reporte a PDF", userId);

        var dashboard = request.IncludeDashboard ? await _reportService.GetDashboardStatsAsync() : null;
        var financial = request.IncludeFinancial ? await _reportService.GetFinancialReportAsync(request.StartDate, request.EndDate) : null;
        var performance = request.IncludePerformance ? await _reportService.GetPerformanceMetricsAsync(request.StartDate, request.EndDate) : null;
        var clients = request.IncludeClients ? await _reportService.GetReportsByClientAsync(request.StartDate, request.EndDate) : null;
        var projects = request.IncludeProjects ? await _reportService.GetReportsByProjectAsync(request.StartDate, request.EndDate) : null;
        var employees = request.IncludeEmployees ? await _reportService.GetReportsByUserAsync(request.StartDate, request.EndDate) : null;

        var pdfBytes = _pdfGenerator.Generate(
            dashboard, financial, performance, clients, projects, employees,
            request.StartDate, request.EndDate);

        return File(pdfBytes, "application/pdf", $"Reporte_Finesoft_{DateTime.Now:yyyyMMdd}.pdf");
    }

    /// <summary>
    /// POST: api/reports/send-email
    /// ✅ Solo requiere reports.send_email — botón solo visible si tiene este permiso
    /// </summary>
    [HttpPost("send-email")]
    [RequirePermission("reports.send_email")]
    public async Task<IActionResult> SendReportEmail([FromBody] SendReportEmailRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "Usuario no encontrado" });

        _logger.LogInformation("📧 Usuario {UserId} enviando reporte por email", userId);

        var dashboard = request.IncludeDashboard ? await _reportService.GetDashboardStatsAsync() : null;
        var financial = request.IncludeFinancial ? await _reportService.GetFinancialReportAsync(request.StartDate, request.EndDate) : null;
        var performance = request.IncludePerformance ? await _reportService.GetPerformanceMetricsAsync(request.StartDate, request.EndDate) : null;
        var clients = request.IncludeClients ? await _reportService.GetReportsByClientAsync(request.StartDate, request.EndDate) : null;
        var projects = request.IncludeProjects ? await _reportService.GetReportsByProjectAsync(request.StartDate, request.EndDate) : null;
        var employees = request.IncludeEmployees ? await _reportService.GetReportsByUserAsync(request.StartDate, request.EndDate) : null;

        var pdfBytes = _pdfGenerator.Generate(
            dashboard, financial, performance, clients, projects, employees,
            request.StartDate, request.EndDate);

        var emailSent = await _emailService.SendReportEmailAsync(
            user.Email!,
            user.UserName ?? "Usuario",
            pdfBytes,
            request.StartDate,
            request.EndDate);

        if (emailSent)
        {
            var pref = await _emailPreferenceService.GetPreferenceAsync(userId);
            if (pref != null)
            {
                var now = DateTime.UtcNow;
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE ReportEmailPreferences SET LastSentAt = {0}, UpdatedAt = {0} WHERE UserId = {1}",
                    now, userId);
            }

            return Ok(new { success = true, message = "Reporte enviado exitosamente" });
        }

        return BadRequest(new { success = false, message = "Error al enviar el reporte por email" });
    }
}