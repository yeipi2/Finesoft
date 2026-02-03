using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class ReportApiService : IReportApiService
{
    private readonly HttpClient _httpClient;

    public ReportApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardStatsDto?> GetDashboardStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DashboardStatsDto>("api/reports/dashboard");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener estadisticas dashboard: {e.Message}");
            throw;
        }
    }

    public async Task<FinancialReportDto?> GetFinancialReportAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var query = BuildDateQuery(startDate, endDate);
            return await _httpClient.GetFromJsonAsync<FinancialReportDto>($"api/reports/financial{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener reporte financiero: {e.Message}");
            throw;
        }
    }

    public async Task<PerformanceMetricsDto?> GetPerformanceMetricsAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var query = BuildDateQuery(startDate, endDate);
            return await _httpClient.GetFromJsonAsync<PerformanceMetricsDto>($"api/reports/performance{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener metricas: {e.Message}");
            throw;
        }
    }

    public async Task<List<RevenueTrendDto>?> GetRevenueTrendAsync(int months = 12)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RevenueTrendDto>>($"api/reports/revenue-trend?months={months}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener tendencia ingresos: {e.Message}");
            throw;
        }
    }

    public async Task<List<TicketStatusChartDto>?> GetTicketsByStatusAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TicketStatusChartDto>>("api/reports/tickets-by-status");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener estatus tickets: {e.Message}");
            throw;
        }
    }

    public async Task<List<ClientReportDto>?> GetReportsByClientAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var query = BuildDateQuery(startDate, endDate);
            return await _httpClient.GetFromJsonAsync<List<ClientReportDto>>($"api/reports/by-client{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener reportes clientes: {e.Message}");
            throw;
        }
    }

    public async Task<List<ProjectReportDto>?> GetReportsByProjectAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var query = BuildDateQuery(startDate, endDate);
            return await _httpClient.GetFromJsonAsync<List<ProjectReportDto>>($"api/reports/by-project{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener reportes proyectos: {e.Message}");
            throw;
        }
    }

    public async Task<List<UserReportDto>?> GetReportsByUserAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var query = BuildDateQuery(startDate, endDate);
            return await _httpClient.GetFromJsonAsync<List<UserReportDto>>($"api/reports/by-user{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener reportes usuarios: {e.Message}");
            throw;
        }
    }

    public async Task<List<TopClientDto>?> GetTopClientsAsync(int top = 10)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TopClientDto>>($"api/reports/top-clients?top={top}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener top clientes: {e.Message}");
            throw;
        }
    }

    private string BuildDateQuery(DateTime? startDate, DateTime? endDate)
    {
        var query = new List<string>();
        if (startDate.HasValue) query.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) query.Add($"endDate={endDate.Value:yyyy-MM-dd}");

        return query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
    }
}