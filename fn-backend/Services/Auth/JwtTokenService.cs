using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace fn_backend.Services;

// Contrato para generar JWTs
public interface IJwtTokenService
{
    Task<string> CreateTokenAsync(IdentityUser user);
}

// Implementación concreta del servicio de JWT
public class JwtTokenService : IJwtTokenService
{
    // Acceso a configuración y a usuarios/roles de Identity
    private readonly IConfiguration _config;
    private readonly UserManager<IdentityUser> _userManager;

    public JwtTokenService(IConfiguration config, UserManager<IdentityUser> userManager)
    {
        // Inyección de dependencias
        _config = config;
        _userManager = userManager;
    }

    // Genera un JWT con claims y roles del usuario
    public async Task<string> CreateTokenAsync(IdentityUser user)
    {
        // Leer configuración JWT desde appsettings
        var jwt = _config.GetSection("Jwt");
        var key = jwt["Key"]!;
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var expiresMinutes = int.Parse(jwt["ExpiresMinutes"] ?? "60");

        // Obtener roles asignados al usuario
        var roles = await _userManager.GetRolesAsync(user);

        // Claims base del token
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // roles -> ClaimTypes.Role (lo que Blazor usa y [Authorize(Roles=...)] entiende)
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // Crear la llave y credenciales de firma
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Construir el token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        // Serializar a string (Bearer token)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
