using fn_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Administracion")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionService permissionService,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todos los permisos disponibles
    /// GET: api/permissions
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PermissionDto>>> GetAllPermissions()
    {
        try
        {
            _logger.LogInformation("📋 Obteniendo todos los permisos...");
            var permissions = await _permissionService.GetAllPermissionsAsync();
            _logger.LogInformation($"✅ {permissions.Count} permisos obtenidos");
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error al obtener permisos: {ex.Message}");
            return StatusCode(500, new { message = "Error al obtener permisos", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener permisos de un rol específico
    /// GET: api/permissions/role/{roleId}
    /// </summary>
    [HttpGet("role/{roleId}")]
    public async Task<ActionResult<RolePermissionsDto>> GetRolePermissions(string roleId)
    {
        try
        {
            _logger.LogInformation($"📋 Obteniendo permisos del rol {roleId}...");
            var rolePermissions = await _permissionService.GetRolePermissionsAsync(roleId);

            if (rolePermissions == null)
            {
                _logger.LogWarning($"⚠️ Rol {roleId} no encontrado");
                return NotFound(new { message = "Rol no encontrado" });
            }

            _logger.LogInformation($"✅ {rolePermissions.PermissionIds.Count} permisos del rol {rolePermissions.RoleName}");
            return Ok(rolePermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error al obtener permisos del rol: {ex.Message}");
            return StatusCode(500, new { message = "Error al obtener permisos del rol", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener permisos de todos los roles
    /// GET: api/permissions/all-roles
    /// </summary>
    [HttpGet("all-roles")]
    public async Task<ActionResult<List<RolePermissionsDto>>> GetAllRolesPermissions()
    {
        try
        {
            _logger.LogInformation("📋 Obteniendo permisos de todos los roles...");
            var rolesPermissions = await _permissionService.GetAllRolesPermissionsAsync();
            _logger.LogInformation($"✅ {rolesPermissions.Count} roles obtenidos");

            foreach (var role in rolesPermissions)
            {
                _logger.LogInformation($"   - {role.RoleName}: {role.PermissionIds.Count} permisos");
            }

            return Ok(rolesPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error al obtener permisos de roles: {ex.Message}");
            return StatusCode(500, new { message = "Error al obtener permisos de roles", error = ex.Message });
        }
    }

    /// <summary>
    /// Asignar permisos a un rol
    /// POST: api/permissions/assign
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Roles = "Admin,Administracion")]
    public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsDto dto)
    {
        try
        {
            _logger.LogInformation($"💾 Asignando {dto.PermissionIds.Count} permisos al rol {dto.RoleId}...");

            var success = await _permissionService.AssignPermissionsToRoleAsync(dto);

            if (!success)
            {
                _logger.LogError("❌ No se pudieron asignar los permisos");
                return BadRequest(new { message = "No se pudieron asignar los permisos" });
            }

            _logger.LogInformation("✅ Permisos asignados correctamente");
            return Ok(new { message = "Permisos asignados correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error al asignar permisos: {ex.Message}");
            return StatusCode(500, new { message = "Error al asignar permisos", error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint de prueba para verificar que el controlador funciona
    /// GET: api/permissions/test
    /// </summary>
    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
        _logger.LogInformation("🧪 Test endpoint llamado");
        return Ok(new
        {
            message = "PermissionsController está funcionando correctamente",
            timestamp = DateTime.UtcNow
        });
    }
}