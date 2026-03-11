using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Util;

namespace fs_backend.Services;

public interface IClientService
{
    Task<IEnumerable<ClientDto>> GetClientsAsync();  // ⭐ Client → ClientDto
    Task<Client?> GetClientByIdAsync(int id);
    Task<ServiceResult<Client>> CreateClientAsync(ClientDto userDto);
    Task<ServiceResult<bool>> UpdateClientAsync(int id, ClientDto dto);
    Task<ServiceResult<bool>> DeleteClientAsync(int id);
    Task<List<ClientDto>> SearchClientsAsync(string query);
}