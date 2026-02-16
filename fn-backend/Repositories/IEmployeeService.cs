using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Util;

namespace fs_backend.Services;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetEmployeesAsync();
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<EmployeeDto?> GetEmployeeByUserIdAsync(string userId);
    Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(EmployeeDto dto);
    Task<ServiceResult<bool>> UpdateEmployeeAsync(int id, EmployeeDto dto);
    Task<ServiceResult<bool>> DeleteEmployeeAsync(int id);
    Task<List<EmployeeDto>> SearchEmployeesAsync(string query);
    Task<(bool Success, string? ErrorMessage)> ToggleEmployeeStatusAsync(int id);

}