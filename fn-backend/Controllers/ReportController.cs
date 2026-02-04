using fs_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]

public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = await _reportService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("by-user")]
    public async Task<IActionResult> GetReportsByUser([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var reports = await _reportService.GetReportsByUserAsync(startDate, endDate);
        return Ok(reports);
    }

    [HttpGet("by-client")]
    public async Task<IActionResult> GetReportsByClient([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var reports = await _reportService.GetReportsByClientAsync(startDate, endDate);
        return Ok(reports);
    }

    [HttpGet("by-project")]
    public async Task<IActionResult> GetReportsByProject([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var reports = await _reportService.GetReportsByProjectAsync(startDate, endDate);
        return Ok(reports);
    }

    [HttpGet("financial")]
    public async Task<IActionResult> GetFinancialReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetFinancialReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformanceMetrics([FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var metrics = await _reportService.GetPerformanceMetricsAsync(startDate, endDate);
        return Ok(metrics);
    }

    [HttpGet("revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] int months = 12)
    {
        var trend = await _reportService.GetRevenueTrendAsync(months);
        return Ok(trend);
    }

    [HttpGet("tickets-by-status")]
    public async Task<IActionResult> GetTicketsByStatus()
    {
        var data = await _reportService.GetTicketsByStatusAsync();
        return Ok(data);
    }

    [HttpGet("top-clients")]
    public async Task<IActionResult> GetTopClients([FromQuery] int top = 10)
    {
        var clients = await _reportService.GetTopClientsAsync(top);
        return Ok(clients);
    }
}