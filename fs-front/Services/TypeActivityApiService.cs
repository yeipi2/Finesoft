using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class TypeActivityApiService : ITypeActivityApiService
{
    private readonly HttpClient _httpClient;

    public TypeActivityApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TypeActivityDto>?> GetTypeActivitiesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TypeActivityDto>>("api/typeactivities");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener tipos de actividad: {e.Message}");
            return null;
        }
    }

    public async Task<TypeActivityDto?> GetTypeActivityByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TypeActivityDto>($"api/typeactivities/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener tipo de actividad {id}: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, TypeActivityDto? CreatedTypeActivity, string? ErrorMessage)>
        CreateTypeActivityAsync(TypeActivityDto typeActivity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/typeactivities", typeActivity);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<TypeActivityDto>();
                return (true, created, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear tipo de actividad: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateTypeActivityAsync(int id,
        TypeActivityDto typeActivity)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/typeactivities/{id}", typeActivity);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar tipo de actividad: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteTypeActivityAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/typeactivities/{id}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar tipo de actividad: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }
}