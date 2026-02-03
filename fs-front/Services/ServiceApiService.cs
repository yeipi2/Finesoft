using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class ServiceApiService : IServiceApiService
{
    private readonly HttpClient _httpClient;

    public ServiceApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ServiceDetailDto>?> GetServicesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ServiceDetailDto>>("api/services");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener servicios: {e.Message}");
            return null;
        }
    }

    public async Task<List<ServiceDetailDto>?> GetServicesByProjectAsync(int projectId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ServiceDetailDto>>($"api/services/project/{projectId}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener servicios del proyecto: {e.Message}");
            return null;
        }
    }

    public async Task<ServiceDetailDto?> GetServiceByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ServiceDetailDto>($"api/services/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener servicio {id}: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, ServiceDetailDto? CreatedService, string? ErrorMessage)> CreateServiceAsync(
        ServiceDto service)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/services", service);
            if (response.IsSuccessStatusCode)
            {
                var createdService = await response.Content.ReadFromJsonAsync<ServiceDetailDto>();
                return (true, createdService, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear servicio: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateServiceAsync(int id, ServiceDto service)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/services/{id}", service);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar servicio: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteServiceAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/services/{id}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar servicio: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }
}