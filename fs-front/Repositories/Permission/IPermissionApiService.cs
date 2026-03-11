// ============================================
// fs-front/Repositories/IPermissionApiService.cs
// ============================================
using fs_front.DTO;
using fs_front.Services;
using System.Net.Http.Json;


namespace fs_front.Repositories;

public interface IPermissionApiService
{
    Task<List<PermissionDto>?> GetAllPermissionsAsync();
    Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleId);
    Task<List<RolePermissionsDto>?> GetAllRolesPermissionsAsync();
    Task<(bool Success, string? ErrorMessage)> AssignPermissionsAsync(AssignPermissionsDto dto);
}