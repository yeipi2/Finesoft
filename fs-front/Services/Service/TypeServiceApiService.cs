using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class TypeServiceApiService : ITypeServiceApiService
{
    private readonly HttpClient _httpClient;

    public TypeServiceApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TypeServiceDto>?> GetTypeServicesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TypeServiceDto>>("api/typeservices");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener tipos de servicio: {e.Message}");
            return null;
        }
    }

    public async Task<TypeServiceDto?> GetTypeServiceByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TypeServiceDto>($"api/typeservices/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener tipo de servicio {id}: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, TypeServiceDto? CreatedTypeService, string? ErrorMessage)> CreateTypeServiceAsync(
        TypeServiceDto typeService)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/typeservices", typeService);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<TypeServiceDto>();
                return (true, created, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear tipo de servicio: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateTypeServiceAsync(int id, TypeServiceDto typeService)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/typeservices/{id}", typeService);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar tipo de servicio: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteTypeServiceAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/typeservices/{id}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar tipo de servicio: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }
}