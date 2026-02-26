using fn_backend.DTO;
using fn_backend.Services;
using fs_backend.Attributes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace fn_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/users
    /// ⭐ CAMBIO: Permitir a usuarios autenticados ver la lista
    /// (necesario para dropdowns de asignación en formularios)
    /// Pero si es Administracion, filtrar Admins
    /// </summary>
    [HttpGet]
    [Authorize] // Solo requiere autenticación
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo usuarios", userId);

        var users = await _userService.GetUsersAsync();

        // ✅ Si es Administracion, FILTRAR usuarios Admin
        if (User.IsInRole("Administracion"))
        {
            users = users.Where(u => u.RoleName != "Admin").ToList();
        }

        // ✅ Si es Empleado, solo mostrar usuarios para referencia (sin datos sensibles)
        if (User.IsInRole("Empleado"))
        {
            // Opcional: podrías filtrar o limitar la info aquí
            // Por ahora dejamos que vean la lista para el dropdown de asignación
        }

        return Ok(users);
    }

    /// <summary>
    /// GET: api/users/{id}
    /// Requiere permiso: usuarios.view
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("usuarios.view")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        // ✅ Si es Administracion y el usuario es Admin, bloquear
        if (User.IsInRole("Administracion") && user.RoleName == "Admin")
        {
            return Forbid(); // 403 Forbidden
        }

        return Ok(user);
    }

    /// <summary>
    /// POST: api/users
    /// Requiere permiso: usuarios.create
    /// </summary>
    [HttpPost]
    [RequirePermission("usuarios.create")]
    public async Task<ActionResult<UserDto>> CreateUser(UserDto userDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} creando usuario", userId);

        // ✅ Si es Administracion y quiere crear un Admin, bloquear
        if (User.IsInRole("Administracion") && userDto.RoleName == "Admin")
        {
            return Forbid();
        }

        var result = await _userService.CreateUserAsync(userDto);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/users/{id}
    /// Requiere permiso: usuarios.edit
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("usuarios.edit")]
    public async Task<IActionResult> UpdateUser(string id, UserDto updateUserDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} actualizando usuario {TargetUserId}", userId, id);

        var existingUser = await _userService.GetUserByIdAsync(id);
        if (existingUser == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        if (User.IsInRole("Administracion") && existingUser.RoleName == "Admin")
        {
            return Forbid();
        }

        if (User.IsInRole("Administracion") && updateUserDto.RoleName == "Admin")
        {
            return Forbid();
        }

        var result = await _userService.UpdateUserAsync(id, updateUserDto);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("no encontrado")))
                return NotFound(result.Errors);

            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// DELETE: api/users/{id}
    /// Requiere permiso: usuarios.delete
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("usuarios.delete")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} eliminando usuario {TargetUserId}", userId, id);

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        if (User.IsInRole("Administracion") && user.RoleName == "Admin")
        {
            return Forbid();
        }

        var result = await _userService.DeleteUserAsync(id);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("no encontrado")))
                return NotFound(result.Errors);

            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// POST: api/users/{id}/change-password
    /// Requiere permiso: usuarios.edit
    /// </summary>
    [HttpPost("{id}/change-password")]
    [RequirePermission("usuarios.edit")]
    public async Task<IActionResult> ChangePassword(string id, ChangePasswordDto changePasswordDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} cambiando contraseña de {TargetUserId}", userId, id);

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        if (User.IsInRole("Administracion") && user.RoleName == "Admin")
        {
            return Forbid();
        }

        var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("no encontrado")))
                return NotFound(result.Errors);

            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Contraseña cambiada exitosamente" });
    }
    /// GET: api/users/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var profile = await _userService.GetMyProfileAsync(userId);
        if (profile is null) return NotFound();

        return Ok(profile);
    }

    /// PUT: api/users/me
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile(ProfileUpdateDto dto)
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        Console.WriteLine($"[DEBUG] userId={userId} role='{role}'");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userService.UpdateMyProfileAsync(userId, role, dto);
        return result.Succeeded ? Ok(new { message = "Perfil actualizado" }) : BadRequest(result.Errors);
    }

    /// POST: api/users/me/change-password
    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userService.ChangePasswordAsync(userId, dto);
        return result.Succeeded ? Ok(new { message = "Contraseña cambiada exitosamente" }) : BadRequest(result.Errors);
    }
}