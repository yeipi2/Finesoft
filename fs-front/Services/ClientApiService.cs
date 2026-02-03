using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class ClientApiService : IClientApiService
{
    private readonly HttpClient _httpClient;

    public ClientApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ClientDto>?> GetClientsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ClientDto>>("api/clients");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener clientes: {e.Message}");
            throw;
        }
    }

    public async Task<ClientDto?> GetClientByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ClientDto>($"api/clients/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener cliente {id}: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, ClientDto? CreatedClient, string? ErrorMessage)> CreateClientAsync(
        ClientDto client)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/clients", client);

            if (response.IsSuccessStatusCode)
            {
                var createdClient = await response.Content.ReadFromJsonAsync<ClientDto>();
                return (true, createdClient, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear cliente: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateClientAsync(int id, ClientDto client)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/clients/{id}", client);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar cliente: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteClientAsync(int? id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/clients/{id}");

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar cliente: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }
}