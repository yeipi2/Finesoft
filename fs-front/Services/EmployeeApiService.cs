using System.Net.Http.Json;
using fs_front.DTO;
using fs_front.Repositories;

namespace fs_front.Services;

public class EmployeeApiService : IEmployeeApiService
{
    private readonly HttpClient _httpClient;

    public EmployeeApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EmployeeDto>?> GetEmployeesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<EmployeeDto>>("api/employees");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener empleados: {e.Message}");
            throw;
        }
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmployeeDto>($"api/employees/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener empleado {id}: {e.Message}");
            return null;
        }
    }

    public async Task<EmployeeDto?> GetEmployeeByUserIdAsync(string userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmployeeDto>($"api/employees/user/{userId}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener empleado por userId {userId}: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, EmployeeDto? CreatedEmployee, string? ErrorMessage)> CreateEmployeeAsync(
        EmployeeDto employee)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/employees", employee);

            if (response.IsSuccessStatusCode)
            {
                var createdEmployee = await response.Content.ReadFromJsonAsync<EmployeeDto>();
                return (true, createdEmployee, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear empleado: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateEmployeeAsync(int id, EmployeeDto employee)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/employees/{id}", employee);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar empleado: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteEmployeeAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/employees/{id}");

            if (response.IsSuccessStatusCode)
                return (true, null);

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar empleado: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    /// <summary>
    /// Cambia el estado del empleado. Retorna también cuántos tickets fueron desasignados
    /// para que el componente pueda mostrar una notificación informativa.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage, int UnassignedTickets)> ToggleEmployeeStatusAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/employees/toggle-status/{id}", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ToggleStatusResult>();
                return (true, null, result?.UnassignedTickets ?? 0);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al cambiar estado: {errorContent}", 0);
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}", 0);
        }
    }

    public async Task<List<EmployeeDto>> SearchEmployeesAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<EmployeeDto>();

            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"api/employees/search?query={encodedQuery}");

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<EmployeeDto>>() ?? new List<EmployeeDto>();

            return new List<EmployeeDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al buscar empleados: {ex.Message}");
            return new List<EmployeeDto>();
        }
    }

    // DTO interno para deserializar la respuesta del toggle
    private class ToggleStatusResult
    {
        public int UnassignedTickets { get; set; }
    }
}