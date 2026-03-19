using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Util;

namespace fs_backend.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectDetailDto>> GetProjectsAsync();
    Task<(List<ProjectDetailDto> Items, int Total)> GetProjectsPaginatedAsync(
        string? search = null,
        string? sortField = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 20);
    Task<ProjectDetailDto?> GetProjectByIdAsync(int id);
    Task<ServiceResult<Project>> CreateProjectAsync(ProjectDto projectDto);
    Task<ServiceResult<bool>> UpdateProjectAsync(int id, ProjectDto updateProjectDto);
    Task<ServiceResult<bool>> DeleteProjectAsync(int id);
    Task<IEnumerable<ProjectDetailDto>> GetProjectsByClientIdAsync(int clientId);
    Task<IEnumerable<ProjectDetailDto>> GetProjectsByUserEmailAsync(string email);
}