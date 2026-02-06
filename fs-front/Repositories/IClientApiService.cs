using fs_front.DTO;

public interface IClientApiService
{
    Task<List<ClientDto>?> GetClientsAsync();
    Task<ClientDto?> GetClientByIdAsync(int id);
    Task<(bool Success, ClientDto? CreatedClient, string? ErrorMessage)> CreateClientAsync(ClientDto client);
    Task<(bool Success, string? ErrorMessage)> UpdateClientAsync(int id, ClientDto client);
    Task<(bool Success, string? ErrorMessage)> DeleteClientAsync(int? id);
    Task<List<ClientDto>> SearchClientsAsync(string query);
}