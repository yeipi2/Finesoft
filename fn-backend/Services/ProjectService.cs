using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Util;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProjectDetailDto>> GetProjectsAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Services)
            .ThenInclude(s => s.TypeService)
            .Include(p => p.Services)
            .ThenInclude(s => s.TypeActivity)
            .ToListAsync();

        return projects.Select(MapToDetailDto);
    }

    public async Task<ProjectDetailDto?> GetProjectByIdAsync(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Services)
            .ThenInclude(s => s.TypeService)
            .Include(p => p.Services)
            .ThenInclude(s => s.TypeActivity)
            .FirstOrDefaultAsync(p => p.Id == id);

        return project == null ? null : MapToDetailDto(project);
    }

    public async Task<ServiceResult<Project>> CreateProjectAsync(ProjectDto projectDto)
    {
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == projectDto.ClientId);
        if (!clientExists)
        {
            return ServiceResult<Project>.Failure("El cliente especificado no existe");
        }

        var project = new Project
        {
            Name = projectDto.Name,
            Description = projectDto.Description,
            ClientId = projectDto.ClientId,
            IsActive = projectDto.IsActive
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return ServiceResult<Project>.Success(project);
    }

    public async Task<ServiceResult<bool>> UpdateProjectAsync(int id, ProjectDto updateProjectDto)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            return ServiceResult<bool>.Failure("Proyecto no encontrado");
        }

        // Verificar que el cliente existe
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == updateProjectDto.ClientId);
        if (!clientExists)
        {
            return ServiceResult<bool>.Failure("El cliente especificado no existe");
        }

        project.Name = updateProjectDto.Name;
        project.Description = updateProjectDto.Description;
        project.ClientId = updateProjectDto.ClientId;
        project.IsActive = updateProjectDto.IsActive;

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteProjectAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            return ServiceResult<bool>.Failure("Proyecto no encontrado");
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    private ProjectDetailDto MapToDetailDto(Project project)
    {
        return new ProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            ClientId = project.ClientId,
            IsActive = project.IsActive,
            Client = project.Client == null
                ? null
                : new ClientDto
                {
                    Id = project.Client.Id,
                    CompanyName = project.Client.CompanyName,
                    ContactName = project.Client.ContactName,
                    Email = project.Client.Email,
                    Phone = project.Client.Phone,
                    RFC = project.Client.RFC,
                    Address = project.Client.Address,
                    ServiceMode = project.Client.ServiceMode,
                    MonthlyRate = project.Client.MonthlyRate,
                    IsActive = project.Client.IsActive
                },
            Services = project.Services?.Select(s => new ServiceDetailDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                HourlyRate = s.HourlyRate,
                IsActive = s.IsActive,
                ProjectId = s.ProjectId,
                ProjectName = project.Name,
                TypeServiceId = s.TypeServiceId,
                TypeServiceName = s.TypeService?.Name ?? string.Empty,
                TypeActivityId = s.TypeActivityId,
                TypeActivityName = s.TypeActivity?.Name ?? string.Empty
            }).ToList()
        };
    }
}