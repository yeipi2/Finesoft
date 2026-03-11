using fn_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICacheService _cache;

    public PermissionService(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        ICacheService cache)
    {
        _context = context;
        _roleManager = roleManager;
        _cache = cache;
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.AllPermissions,
            async () =>
            {
                return await _context.Permissions
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Action)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Module = p.Module,
                        Action = p.Action,
                        Description = p.Description,
                        Code = p.Code
                    })
                    .ToListAsync();
            },
            TimeSpan.FromHours(1) // Cache de 1 hora para permisos
        ) ?? new List<PermissionDto>();
    }

    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleId)
    {
        var cacheKey = string.Format(CacheKeys.RolePermissions, roleId);

        return await _cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null) return null;

                var permissionIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                return new RolePermissionsDto
                {
                    RoleId = roleId,
                    RoleName = role.Name ?? "",
                    PermissionIds = permissionIds
                };
            },
            TimeSpan.FromMinutes(30)
        );
    }

    public async Task<List<RolePermissionsDto>> GetAllRolesPermissionsAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var result = new List<RolePermissionsDto>();

        foreach (var role in roles)
        {
            var permissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            result.Add(new RolePermissionsDto
            {
                RoleId = role.Id,
                RoleName = role.Name ?? "",
                PermissionIds = permissionIds
            });
        }

        return result;
    }

    public async Task<bool> AssignPermissionsToRoleAsync(AssignPermissionsDto dto)
    {
        try
        {
            // Eliminar permisos actuales
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == dto.RoleId)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(existingPermissions);

            // Agregar nuevos permisos
            var newPermissions = dto.PermissionIds.Select(permId => new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = permId
            }).ToList();

            await _context.RolePermissions.AddRangeAsync(newPermissions);
            await _context.SaveChangesAsync();

            // Invalidar cache de permisos del rol
            await _cache.InvalidateAsync(string.Format(CacheKeys.RolePermissions, dto.RoleId));

            return true;
        }
        catch
        {
            return false;
        }
    }
}
