using fs_front.DTO;

namespace fs_front.Repositories;

public interface ISupervisorApiService
{
    Task<PaginatedResult<EmployeeSummaryDto>> GetEmployeesAsync(
        int page = 1, 
        int pageSize = 20, 
        string? search = null,
        string? departmentFilter = null,
        string? positionFilter = null,
        string? statusFilter = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);

    Task<PaginatedResult<EmployeeActionDto>> GetEmployeeHistoryAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        EmployeeActionType? actionTypeFilter = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);

    Task<EmployeeStatsDto?> GetEmployeeStatsAsync(string userId);
    Task<EmployeeSummaryDto?> GetEmployeeDetailsAsync(string userId);
    Task<SupervisorFiltersDto?> GetFiltersAsync();
}
