// ============================================
// UBICACIÓN: fn-backend/Models/Permission.cs
// Copiar este archivo a tu proyecto
// ============================================
namespace fs_backend.Models;

public class Permission
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class RolePermission
{
    public int Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}