using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using fs_backend.Identity; // ⭐ AGREGAR
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // ⭐ AGREGAR
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace fn_backend.Services;

public interface IJwtTokenService
{
    Task<string> CreateTokenAsync(IdentityUser user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager; // ⭐ AGREGAR
    private readonly ApplicationDbContext _context;          // ⭐ AGREGAR

    public JwtTokenService(
        IConfiguration config,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager, // ⭐ AGREGAR
        ApplicationDbContext context)           // ⭐ AGREGAR
    {
        _config = config;
        _userManager = userManager;
        _roleManager = roleManager; // ⭐ AGREGAR
        _context = context;         // ⭐ AGREGAR
    }

    public async Task<string> CreateTokenAsync(IdentityUser user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = jwt["Key"]!;
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var expiresMinutes = int.Parse(jwt["ExpiresMinutes"] ?? "60");

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // ⭐ NUEVO: Cargar permisos desde SQL Server usando tu estructura existente
        var userRole = roles.FirstOrDefault();
        if (userRole != null)
        {
            var roleEntity = await _roleManager.FindByNameAsync(userRole);
            if (roleEntity != null)
            {
                // Usa tu tabla RolePermissions y el campo Code de Permission
                var permissionCodes = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleEntity.Id)
                    .Include(rp => rp.Permission)
                    .Select(rp => rp.Permission.Code)
                    .ToListAsync();

                foreach (var code in permissionCodes)
                    claims.Add(new Claim("permission", code));
            }
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}