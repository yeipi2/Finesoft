// ============================================
// BACKEND REPOSITORY - fn-backend/Repositories/IPermissionService.cs
// ============================================
using fn_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using Microsoft.AspNetCore.Identity;

namespace fs_backend.Repositories;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleId);
    Task<bool> AssignPermissionsToRoleAsync(AssignPermissionsDto dto);
    Task<List<RolePermissionsDto>> GetAllRolesPermissionsAsync();
}

