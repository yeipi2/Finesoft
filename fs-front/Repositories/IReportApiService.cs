using fs_front.DTO;

namespace fs_front.Services;

public interface IReportApiService
{
    Task<DashboardStatsDto?> GetDashboardStatsAsync();
    Task<FinancialReportDto?> GetFinancialReportAsync(DateTime? startDate, DateTime? endDate);
    Task<PerformanceMetricsDto?> GetPerformanceMetricsAsync(DateTime? startDate, DateTime? endDate);
    Task<List<RevenueTrendDto>?> GetRevenueTrendAsync(int months = 12);
    Task<List<TicketStatusChartDto>?> GetTicketsByStatusAsync();
    Task<List<ClientReportDto>?> GetReportsByClientAsync(DateTime? startDate, DateTime? endDate);
    Task<List<ProjectReportDto>?> GetReportsByProjectAsync(DateTime? startDate, DateTime? endDate);
    Task<List<UserReportDto>?> GetReportsByUserAsync(DateTime? startDate, DateTime? endDate);
    Task<List<TopClientDto>?> GetTopClientsAsync(int top = 10);
    Task<PublicStatsDto?> GetPublicStatsAsync();
}