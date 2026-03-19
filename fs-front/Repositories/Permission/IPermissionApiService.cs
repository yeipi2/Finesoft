using fs_front.DTO;

namespace fs_front.Repositories;

public interface IPermissionApiService
{
    Task<List<PermissionDto>?> GetAllPermissionsAsync();
    Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleId);
    Task<List<RolePermissionsDto>?> GetAllRolesPermissionsAsync();
    Task<(bool Success, string? ErrorMessage)> AssignPermissionsAsync(AssignPermissionsDto dto);
}