using fs_front.DTO;

namespace fs_front.Services;

public interface IProjectApiService
{
    Task<List<ProjectDetailDto>?> GetProjectsAsync();
    Task<ProjectDetailDto?> GetProjectByIdAsync(int id);
    Task<(bool Success, ProjectDto? CreatedProject, string? ErrorMessage)> CreateProjectAsync(ProjectDto project);
    Task<(bool Success, string? ErrorMessage)> UpdateProjectAsync(int id, ProjectDto project);
    Task<(bool Success, string? ErrorMessage)> DeleteProjectAsync(int id);
}