// fs-backend/Services/ProjectService.cs — COMPLETO ACTUALIZADO
// Calcula MonthlyHoursUsed en el cliente para que el form de tickets muestre horas disponibles

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
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);

        var projects = await _context.Projects
            .Include(p => p.Client)
            .ToListAsync();

        // ── Calcular horas usadas por cliente mensual en una sola query ────────
        var monthlyClientIds = projects
            .Where(p => p.Client?.ServiceMode == "Mensual" || p.Client?.BillingFrequency == "Monthly")
            .Select(p => p.ClientId)
            .Distinct()
            .ToList();

        // Mapa clientId → lista de projectIds
        var clientProjectMap = projects
            .GroupBy(p => p.ClientId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToList());

        // Una sola query de tickets para todos los proyectos mensuales
        var hoursPerProject = new Dictionary<int, decimal>();
        if (monthlyClientIds.Any())
        {
            var allMonthlyProjectIds = clientProjectMap
                .Where(kv => monthlyClientIds.Contains(kv.Key))
                .SelectMany(kv => kv.Value)
                .ToList();

            if (allMonthlyProjectIds.Any())
            {
                var hoursSums = await _context.Tickets
                    .Where(t => t.ProjectId.HasValue
                             && allMonthlyProjectIds.Contains(t.ProjectId.Value)
                             && t.UpdatedAt >= currentMonth
                             && t.UpdatedAt < nextMonth)
                    .GroupBy(t => t.ProjectId!.Value)
                    .Select(g => new { ProjectId = g.Key, Hours = g.Sum(t => t.ActualHours) })
                    .ToListAsync();

                foreach (var h in hoursSums)
                    hoursPerProject[h.ProjectId] = h.Hours;
            }
        }

        // Suma de horas por cliente
        var hoursPerClient = new Dictionary<int, decimal>();
        foreach (var clientId in monthlyClientIds)
        {
            if (clientProjectMap.TryGetValue(clientId, out var pIds))
            {
                hoursPerClient[clientId] = pIds
                    .Sum(pid => hoursPerProject.GetValueOrDefault(pid, 0));
            }
        }

        return projects.Select(p =>
            MapToDetailDto(p, hoursPerClient.GetValueOrDefault(p.ClientId, 0)));
    }

    public async Task<ProjectDetailDto?> GetProjectByIdAsync(int id)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);

        var project = await _context.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return null;

        decimal hoursUsed = 0;
        if (project.Client?.ServiceMode == "Mensual" || project.Client?.BillingFrequency == "Monthly")
        {
            var clientProjectIds = await _context.Projects
                .Where(p => p.ClientId == project.ClientId)
                .Select(p => p.Id)
                .ToListAsync();

            if (clientProjectIds.Any())
            {
                hoursUsed = await _context.Tickets
                    .Where(t => t.ProjectId.HasValue
                             && clientProjectIds.Contains(t.ProjectId.Value)
                             && t.UpdatedAt >= currentMonth
                             && t.UpdatedAt < nextMonth)
                    .SumAsync(t => t.ActualHours);
            }
        }

        return MapToDetailDto(project, hoursUsed);
    }

    public async Task<ServiceResult<Project>> CreateProjectAsync(ProjectDto projectDto)
    {
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == projectDto.ClientId);
        if (!clientExists)
            return ServiceResult<Project>.Failure("El cliente especificado no existe");

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
            return ServiceResult<bool>.Failure("Proyecto no encontrado");

        var clientExists = await _context.Clients.AnyAsync(c => c.Id == updateProjectDto.ClientId);
        if (!clientExists)
            return ServiceResult<bool>.Failure("El cliente especificado no existe");

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
            return ServiceResult<bool>.Failure("Proyecto no encontrado");

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    // ── Mapper con MonthlyHoursUsed ───────────────────────────────────────────
    private static ProjectDetailDto MapToDetailDto(Project project, decimal monthlyHoursUsed = 0)
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
                    MonthlyHours = project.Client.MonthlyHours,       // ⭐ NUEVO
                    MonthlyHoursUsed = monthlyHoursUsed,               // ⭐ NUEVO calculado
                    IsActive = project.Client.IsActive
                }
        };
    }
}