using Asp.Versioning;
using fn_backend.DTO;
using fs_backend.Services;
using fs_backend.Attributes;
using fs_backend.DTO.Common;
using fs_backend.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/projects
    /// ⭐ CAMBIO: Cualquier usuario autenticado puede ver la lista (para dropdowns en tickets)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetProjects([FromQuery] PaginationQueryDto query)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo proyectos", userId);

        var sortDescending = string.IsNullOrEmpty(query.Sort) || !query.Sort.StartsWith("-");
        var sortField = sortDescending ? query.Sort : query.Sort.Substring(1);

        var (projects, total) = await _projectService.GetProjectsPaginatedAsync(
            search: query.Search,
            sortField: string.IsNullOrEmpty(sortField) ? "name" : sortField,
            sortDescending: sortDescending,
            page: query.NormalizedPage,
            pageSize: query.NormalizedPageSize
        );

        var pagedResult = PaginatedResponseDto<ProjectDetailDto>.Create(projects, total, query.NormalizedPage, query.NormalizedPageSize);
        return Ok(pagedResult);
    }

    /// <summary>
    /// GET: api/projects/by-client/{clientId}
    /// Obtiene los proyectos de un cliente específico (para tickets de clientes)
    /// </summary>
    [HttpGet("by-client/{clientId}")]
    [Authorize]
    public async Task<IActionResult> GetProjectsByClient(int clientId)
    {
        _logger.LogInformation("✅ Obteniendo proyectos del cliente {ClientId}", clientId);
        var projects = await _projectService.GetProjectsByClientIdAsync(clientId);
        var projectList = projects?.ToList() ?? new List<ProjectDetailDto>();
        _logger.LogInformation("✅ Proyectos encontrados para cliente {ClientId}: {Count}", clientId, projectList.Count);
        foreach (var p in projectList)
        {
            _logger.LogInformation("   - Proyecto: {Id} - {Name}", p.Id, p.Name);
        }
        return Ok(projectList);
    }

    /// <summary>
    /// GET: api/projects/by-user-email/{email}
    /// Obtiene los proyectos del cliente por su email
    /// </summary>
    [HttpGet("by-user-email/{email}")]
    [Authorize]
    public async Task<IActionResult> GetProjectsByUserEmail(string email)
    {
        _logger.LogInformation("✅ Obteniendo proyectos para email {Email}", email);
        var projects = await _projectService.GetProjectsByUserEmailAsync(email);
        var projectList = projects?.ToList() ?? new List<ProjectDetailDto>();
        _logger.LogInformation("✅ Proyectos encontrados para email {Email}: {Count}", email, projectList.Count);
        foreach (var p in projectList)
        {
            _logger.LogInformation("   - Proyecto: {Id} - {Name}", p.Id, p.Name);
        }
        return Ok(projectList);
    }

    /// <summary>
    /// GET: api/projects/{id}
    /// Requiere permiso: projects.view_detail
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("projects.view_detail")]
    public async Task<IActionResult> GetProjectById(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
        {
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Proyecto no encontrado");
        }

        return Ok(project);
    }

    /// <summary>
    /// POST: api/projects
    /// Requiere permiso: projects.create
    /// </summary>
    [HttpPost]
    [RequirePermission("projects.create")]
    public async Task<IActionResult> CreateProject(ProjectDto projectDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} creando proyecto", userId);

        var result = await _projectService.CreateProjectAsync(projectDto);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
        }

        return CreatedAtAction(nameof(GetProjectById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/projects/{id}
    /// Requiere permiso: projects.edit
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("projects.edit")]
    public async Task<IActionResult> UpdateProject(int id, ProjectDto projectDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} actualizando proyecto {ProjectId}", userId, id);

        var result = await _projectService.UpdateProjectAsync(id, projectDto);
        if (!result.Succeeded)
        {
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", result.Errors.FirstOrDefault() ?? "Proyecto no encontrado");
        }

        return NoContent();
    }

    /// <summary>
    /// DELETE: api/projects/{id}
    /// Requiere permiso: projects.delete
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("projects.delete")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} eliminando proyecto {ProjectId}", userId, id);

        var result = await _projectService.DeleteProjectAsync(id);
        if (!result.Succeeded)
        {
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", result.Errors.FirstOrDefault() ?? "Proyecto no encontrado");
        }

        return NoContent();
    }
}
