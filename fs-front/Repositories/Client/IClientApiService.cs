using fs_front.DTO;

public interface IClientApiService
{
    Task<List<ClientDto>?> GetClientsAsync();
    Task<PaginatedResponseDto<ClientDto>?> GetClientsPaginatedAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        string? sortField = null,
        bool sortDescending = false);
    Task<ClientDto?> GetClientByIdAsync(int id);
    Task<(bool Success, ClientDto? CreatedClient, string? ErrorMessage)> CreateClientAsync(ClientDto client);
    Task<(bool Success, string? ErrorMessage)> UpdateClientAsync(int id, ClientDto client);
    Task<(bool Success, string? ErrorMessage)> DeleteClientAsync(int? id);
    Task<List<ClientDto>> SearchClientsAsync(string query);
}