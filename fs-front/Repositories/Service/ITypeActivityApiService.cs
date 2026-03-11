using fs_front.DTO;

namespace fs_front.Services;

public interface ITypeActivityApiService
{
    Task<List<TypeActivityDto>?> GetTypeActivitiesAsync();
    Task<TypeActivityDto?> GetTypeActivityByIdAsync(int id);

    Task<(bool Success, TypeActivityDto? CreatedTypeActivity, string? ErrorMessage)> CreateTypeActivityAsync(
        TypeActivityDto typeActivity);

    Task<(bool Success, string? ErrorMessage)> UpdateTypeActivityAsync(int id, TypeActivityDto typeActivity);
    Task<(bool Success, string? ErrorMessage)> DeleteTypeActivityAsync(int id);
}