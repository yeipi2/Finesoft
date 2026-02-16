using System.ComponentModel.DataAnnotations;
using fn_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace fn_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IJwtTokenService _jwt;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IJwtTokenService jwt)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwt = jwt;
    }

    public record LoginRequest([Required] string Email, [Required] string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });

        var token = await _jwt.CreateTokenAsync(user);

        return Ok(new { accessToken = token });
    }

    [HttpPost("refresh")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Refresh()
    {
        // ✅ Mejor: usar el UserId del token
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        IdentityUser? user = null;

        if (!string.IsNullOrWhiteSpace(userId))
            user = await _userManager.FindByIdAsync(userId);

        // fallback: email
        if (user is null)
        {
            var email =
                User.FindFirstValue(ClaimTypes.Email) ??
                User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { message = "No se pudo identificar al usuario" });

            user = await _userManager.FindByEmailAsync(email);
        }

        if (user is null)
            return Unauthorized(new { message = "Usuario no encontrado" });

        var token = await _jwt.CreateTokenAsync(user);
        return Ok(new { accessToken = token });
    }
}
