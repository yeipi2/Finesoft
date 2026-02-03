using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class ProjectApiService : IProjectApiService
{
    private readonly HttpClient _httpClient;

    public ProjectApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProjectDetailDto>?> GetProjectsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ProjectDetailDto>>("api/projects");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener proyectos: {e.Message}");
            throw;
        }
    }

    public async Task<ProjectDetailDto?> GetProjectByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProjectDetailDto>($"api/projects/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener proyecto {id}: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, ProjectDto? CreatedProject, string? ErrorMessage)> CreateProjectAsync(
        ProjectDto project)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/projects", project);

            if (response.IsSuccessStatusCode)
            {
                var createdProject = await response.Content.ReadFromJsonAsync<ProjectDto>();
                return (true, createdProject, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear proyecto: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateProjectAsync(int id, ProjectDto project)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/projects/{id}", project);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar proyecto: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteProjectAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/projects/{id}");

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar proyecto: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }
}