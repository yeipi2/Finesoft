using fn_backend.DTO;
using fs_backend.Hubs;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Administracion")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IHubContext<PermissionsHub> _hub;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionService permissionService,
        IHubContext<PermissionsHub> hub,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<PermissionDto>>> GetAllPermissions()
    {
        var permissions = await _permissionService.GetAllPermissionsAsync();
        return Ok(permissions);
    }

    [HttpGet("role/{roleId}")]
    public async Task<ActionResult<RolePermissionsDto>> GetRolePermissions(string roleId)
    {
        var rolePermissions = await _permissionService.GetRolePermissionsAsync(roleId);
        return rolePermissions is null
            ? NotFound(new { message = "Rol no encontrado" })
            : Ok(rolePermissions);
    }

    [HttpGet("all-roles")]
    public async Task<ActionResult<List<RolePermissionsDto>>> GetAllRolesPermissions()
    {
        var rolesPermissions = await _permissionService.GetAllRolesPermissionsAsync();
        return Ok(rolesPermissions);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsDto dto)
    {
        _logger.LogInformation("💾 Asignando {Count} permisos al rol {RoleId}", dto.PermissionIds.Count, dto.RoleId);

        var success = await _permissionService.AssignPermissionsToRoleAsync(dto);
        if (!success)
            return BadRequest(new { message = "No se pudieron asignar los permisos" });

        // ✅ Obtener RoleName del rol editado (para notificar a ESE rol)
        var roleInfo = await _permissionService.GetRolePermissionsAsync(dto.RoleId);
        var roleName = roleInfo?.RoleName;

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            // 📣 Notificar a todos los usuarios conectados con ese rol
            await _hub.Clients.Group($"role:{roleName}")
                .SendAsync("PermissionsChanged", new { role = roleName });

            _logger.LogInformation("📣 Notificación enviada a role:{RoleName}", roleName);
        }
        else
        {
            _logger.LogWarning("⚠️ No se pudo obtener RoleName para RoleId={RoleId}. No se notificó.", dto.RoleId);
        }

        return Ok(new { message = "Permisos asignados correctamente" });
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
        => Ok(new { message = "PermissionsController está funcionando correctamente", timestamp = DateTime.UtcNow });
}
