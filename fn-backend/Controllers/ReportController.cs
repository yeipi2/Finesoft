using fs_backend.Services;
using fs_backend.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
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
    /// Requiere permiso: reports.financial
    /// </summary>
    [HttpGet("financial")]
    [RequirePermission("reports.financial")]
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
    /// Requiere permiso: reports.financial
    /// </summary>
    [HttpGet("revenue-trend")]
    [RequirePermission("reports.financial")]
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
}