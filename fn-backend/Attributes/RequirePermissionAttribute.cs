using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using fs_backend.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Attributes;

/// <summary>
/// Atributo de autorización basado en permisos granulares
/// Uso: [RequirePermission("tickets.view")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permissionCode;

    public RequirePermissionAttribute(string permissionCode)
    {
        _permissionCode = permissionCode;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. Verificar que el usuario esté autenticado
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 2. Obtener el DbContext y UserManager
        var dbContext = context.HttpContext.RequestServices
            .GetRequiredService<ApplicationDbContext>();

        var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<IdentityUser>>();

        // 3. Obtener el usuario actual
        var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 4. Obtener los roles del usuario
        var userRoles = await userManager.GetRolesAsync(user);
        if (!userRoles.Any())
        {
            context.Result = new ForbidResult(); // 403 Forbidden
            return;
        }

        // 5. ⭐ ADMIN TIENE TODOS LOS PERMISOS AUTOMÁTICAMENTE
        if (userRoles.Contains("Admin"))
        {
            return; // Permitir acceso
        }

        // 6. Verificar si el permiso está asignado a alguno de los roles del usuario
        var roleIds = await dbContext.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        var hasPermission = await dbContext.RolePermissions
            .Include(rp => rp.Permission)
            .AnyAsync(rp =>
                roleIds.Contains(rp.RoleId) &&
                rp.Permission.Code == _permissionCode);

        if (!hasPermission)
        {
            // Log para debugging
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<RequirePermissionAttribute>>();

            logger.LogWarning(
                "⛔ Acceso denegado: Usuario {UserId} ({Roles}) no tiene el permiso '{Permission}'",
                userId, string.Join(", ", userRoles), _permissionCode);

            context.Result = new ForbidResult(); // 403 Forbidden
            return;
        }

        // Si llegamos aquí, el usuario tiene el permiso
        return;
    }
}