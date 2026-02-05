using System.ComponentModel.DataAnnotations;
using fn_backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace fn_backend.Controllers;

// Controlador de autenticación para login y emisión de JWT
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Dependencias de Identity y del servicio JWT
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IJwtTokenService _jwt;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IJwtTokenService jwt)
    {
        // Inyección de dependencias
        _signInManager = signInManager;
        _userManager = userManager;
        _jwt = jwt;
    }

    // Modelo del cuerpo de la petición (valida que Email y Password existan)
    public record LoginRequest([Required] string Email, [Required] string Password);

    // Endpoint: POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // Busca el usuario por email (la app usa email como usuario)
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            // Si no existe, retorna 401
            return Unauthorized(new { message = "Correo o contraseÃ±a incorrecto" });
        }

        // Verifica contraseña con Identity
        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            // Si no coincide, retorna 401
            return Unauthorized(new { message = "Correo o contraseÃ±a incorrecto" });
        }

        // Genera el token JWT usando el servicio
        var token = await _jwt.CreateTokenAsync(user);

        // Devuelve el token al front en JSON
        return Ok(new
        {
            accessToken = token
        });
    }
}
