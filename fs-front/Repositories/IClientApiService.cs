using fs_front.DTO;

namespace fs_front.Services;

public interface IClientApiService
{
    Task<List<ClientDto>?> GetClientsAsync();
    Task<ClientDto?> GetClientByIdAsync(int id);
    Task<(bool Success, ClientDto? CreatedClient, string? ErrorMessage)> CreateClientAsync(ClientDto client);
    Task<(bool Success, string? ErrorMessage)> UpdateClientAsync(int id, ClientDto client);
    Task<(bool Success, string? ErrorMessage)> DeleteClientAsync(int? id);
}