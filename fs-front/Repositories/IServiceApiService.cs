using fs_front.DTO;

namespace fs_front.Services;

public interface IServiceApiService
{
    Task<List<ServiceDetailDto>?> GetServicesAsync();
    Task<List<ServiceDetailDto>?> GetServicesByProjectAsync(int projectId);
    Task<ServiceDetailDto?> GetServiceByIdAsync(int id);
    Task<(bool Success, ServiceDetailDto? CreatedService, string? ErrorMessage)> CreateServiceAsync(ServiceDto service);
    Task<(bool Success, string? ErrorMessage)> UpdateServiceAsync(int id, ServiceDto service);
    Task<(bool Success, string? ErrorMessage)> DeleteServiceAsync(int id);
}