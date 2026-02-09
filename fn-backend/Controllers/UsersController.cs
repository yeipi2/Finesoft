using fn_backend.DTO;
using fn_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fn_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// GET: api/users
    /// - Admin: Ve TODOS los usuarios
    /// - Administracion: Ve todos EXCEPTO usuarios con rol Admin
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOrAdministracion")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();

        // ✅ Si es Administracion, FILTRAR usuarios Admin
        if (User.IsInRole("Administracion"))
        {
            users = users.Where(u => u.RoleName != "Admin").ToList();
        }

        return Ok(users);
    }

    /// <summary>
    /// GET: api/users/{id}
    /// - Administracion: NO puede ver detalles de usuarios Admin
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrAdministracion")]
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
    /// - Administracion: NO puede crear usuarios con rol Admin
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOrAdministracion")]
    public async Task<ActionResult<UserDto>> CreateUser(UserDto userDto)
    {
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
    /// - Administracion: NO puede modificar usuarios Admin
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOrAdministracion")]
    public async Task<IActionResult> UpdateUser(string id, UserDto updateUserDto)
    {
        // ✅ Verificar si el usuario a modificar es Admin
        var existingUser = await _userService.GetUserByIdAsync(id);
        if (existingUser == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        // Si es Administracion y quiere modificar un Admin, bloquear
        if (User.IsInRole("Administracion") && existingUser.RoleName == "Admin")
        {
            return Forbid();
        }

        // También bloquear si quiere cambiar el rol a Admin
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
    /// - Administracion: NO puede eliminar usuarios Admin
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOrAdministracion")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        // ✅ Verificar si el usuario a eliminar es Admin
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        // Si es Administracion y quiere eliminar un Admin, bloquear
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
    /// - Administracion: NO puede cambiar contraseña de usuarios Admin
    /// </summary>
    [HttpPost("{id}/change-password")]
    [Authorize(Policy = "AdminOrAdministracion")]
    public async Task<IActionResult> ChangePassword(string id, ChangePasswordDto changePasswordDto)
    {
        // ✅ Verificar si el usuario es Admin
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        // Si es Administracion y quiere cambiar contraseña de Admin, bloquear
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
}