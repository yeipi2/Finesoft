using System.Net.Http.Json;
using fs_front.DTO;
using fs_front.Repositories;

namespace fs_front.Services;

public class SupervisorApiService : ISupervisorApiService
{
    private readonly HttpClient _httpClient;

    public SupervisorApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaginatedResult<EmployeeSummaryDto>> GetEmployeesAsync(
        int page = 1, 
        int pageSize = 20, 
        string? search = null,
        string? departmentFilter = null,
        string? positionFilter = null,
        string? statusFilter = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrWhiteSpace(departmentFilter) && departmentFilter != "Todos")
                queryParams.Add($"departmentFilter={Uri.EscapeDataString(departmentFilter)}");
            if (!string.IsNullOrWhiteSpace(positionFilter) && positionFilter != "Todos")
                queryParams.Add($"positionFilter={Uri.EscapeDataString(positionFilter)}");
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "Todos")
                queryParams.Add($"statusFilter={Uri.EscapeDataString(statusFilter)}");
            if (dateFrom.HasValue)
                queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue)
                queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

            var url = $"api/supervisor/employees?{string.Join("&", queryParams)}";
            return await _httpClient.GetFromJsonAsync<PaginatedResult<EmployeeSummaryDto>>(url) 
                ?? new PaginatedResult<EmployeeSummaryDto>();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener empleados para supervisión: {e.Message}");
            return new PaginatedResult<EmployeeSummaryDto>();
        }
    }

    public async Task<PaginatedResult<EmployeeActionDto>> GetEmployeeHistoryAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        EmployeeActionType? actionTypeFilter = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (actionTypeFilter.HasValue)
                queryParams.Add($"actionTypeFilter={actionTypeFilter.Value}");
            if (dateFrom.HasValue)
                queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue)
                queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

            var url = $"api/supervisor/employees/{userId}/history?{string.Join("&", queryParams)}";
            return await _httpClient.GetFromJsonAsync<PaginatedResult<EmployeeActionDto>>(url)
                ?? new PaginatedResult<EmployeeActionDto>();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener historial del empleado: {e.Message}");
            return new PaginatedResult<EmployeeActionDto>();
        }
    }

    public async Task<EmployeeStatsDto?> GetEmployeeStatsAsync(string userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmployeeStatsDto>($"api/supervisor/employees/{userId}/stats");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener estadísticas del empleado: {e.Message}");
            return null;
        }
    }

    public async Task<EmployeeSummaryDto?> GetEmployeeDetailsAsync(string userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmployeeSummaryDto>($"api/supervisor/employees/{userId}/details");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener detalles del empleado: {e.Message}");
            return null;
        }
    }

    public async Task<SupervisorFiltersDto?> GetFiltersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SupervisorFiltersDto>("api/supervisor/filters");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener filtros: {e.Message}");
            return null;
        }
    }
}
