using fs_front.DTO;

namespace fs_front.Services;

public interface ITypeServiceApiService
{
    Task<List<TypeServiceDto>?> GetTypeServicesAsync();
    Task<TypeServiceDto?> GetTypeServiceByIdAsync(int id);

    Task<(bool Success, TypeServiceDto? CreatedTypeService, string? ErrorMessage)> CreateTypeServiceAsync(
        TypeServiceDto typeService);

    Task<(bool Success, string? ErrorMessage)> UpdateTypeServiceAsync(int id, TypeServiceDto typeService);
    Task<(bool Success, string? ErrorMessage)> DeleteTypeServiceAsync(int id);
}