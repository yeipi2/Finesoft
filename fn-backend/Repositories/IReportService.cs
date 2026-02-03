using fs_backend.DTO;

namespace fs_backend.Services;

public interface IReportService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<List<UserReportDto>> GetReportsByUserAsync(DateTime? startDate, DateTime? endDate);
    Task<List<ClientReportDto>> GetReportsByClientAsync(DateTime? startDate, DateTime? endDate);
    Task<List<ProjectReportDto>> GetReportsByProjectAsync(DateTime? startDate, DateTime? endDate);
    Task<FinancialReportDto> GetFinancialReportAsync(DateTime? startDate, DateTime? endDate);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(DateTime? startDate, DateTime? endDate);
    Task<List<RevenueTrendDto>> GetRevenueTrendAsync(int months);
    Task<List<TicketStatusChartDto>> GetTicketsByStatusAsync();
    Task<List<TopClientDto>> GetTopClientsAsync(int top);
}