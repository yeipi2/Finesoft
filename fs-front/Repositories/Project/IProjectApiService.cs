using fs_front.DTO;

namespace fs_front.Services;

public interface IProjectApiService
{
    Task<List<ProjectDetailDto>?> GetProjectsAsync();
    Task<PaginatedResponseDto<ProjectDetailDto>?> GetProjectsPaginatedAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        string? sortField = null,
        bool sortDescending = false);
    Task<ProjectDetailDto?> GetProjectByIdAsync(int id);
    Task<List<ProjectDetailDto>?> GetProjectsByClientIdAsync(int clientId);
    Task<List<ProjectDetailDto>?> GetProjectsByUserEmailAsync(string email);
    Task<(bool Success, ProjectDto? CreatedProject, string? ErrorMessage)> CreateProjectAsync(ProjectDto project);
    Task<(bool Success, string? ErrorMessage)> UpdateProjectAsync(int id, ProjectDto project);
    Task<(bool Success, string? ErrorMessage)> DeleteProjectAsync(int id);
}