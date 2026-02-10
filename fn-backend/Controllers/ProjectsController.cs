using fn_backend.DTO;
using fs_backend.Services;
using fs_backend.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
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
    public async Task<IActionResult> GetProjects()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo proyectos", userId);

        var projects = await _projectService.GetProjectsAsync();
        return Ok(projects);
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
            return NotFound(new { message = "Proyecto no encontrado" });
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
            return BadRequest(result.Errors);
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
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Proyecto actualizado exitosamente" });
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
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Proyecto eliminado exitosamente" });
    }
}