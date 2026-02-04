using fn_backend.DTO;
using fn_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace fn_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]

public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"Usuario con ID {id} no encontrado");
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(UserDto userDto)
    {
        var result = await _userService.CreateUserAsync(userDto);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // usa el id del DTO que devuelve el servicio
        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UserDto updateUserDto)
    {
        var result = await _userService.UpdateUserAsync(id, updateUserDto);
        if (!result.Succeeded)
        {
            // chequea si el error fue porque no se encontró el usuario
            if (result.Errors.Any(e => e.Contains("no encontrado")))
                return NotFound(result.Errors);

            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("no encontrado")))
                return NotFound(result.Errors);

            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(string id, ChangePasswordDto changePasswordDto)
    {
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