using fs_front.DTO;

namespace fs_front.Repositories;

public interface IEmployeeApiService
{
    Task<List<EmployeeDto>?> GetEmployeesAsync();
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<EmployeeDto?> GetEmployeeByUserIdAsync(string userId);
    Task<(bool Success, EmployeeDto? CreatedEmployee, string? ErrorMessage)> CreateEmployeeAsync(EmployeeDto employee);
    Task<(bool Success, string? ErrorMessage)> UpdateEmployeeAsync(int id, EmployeeDto employee);
    Task<(bool Success, string? ErrorMessage)> DeleteEmployeeAsync(int id);

    Task<(bool Success, string? ErrorMessage, int UnassignedTickets)> ToggleEmployeeStatusAsync(int id);

    Task<List<EmployeeDto>> SearchEmployeesAsync(string query);
}