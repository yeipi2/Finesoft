
namespace fn_backend.DTO;

public class PermissionDto
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class RolePermissionsDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new();
}

public class AssignPermissionsDto
{
    public string RoleId { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new();
}
