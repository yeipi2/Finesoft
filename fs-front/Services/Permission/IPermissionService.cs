namespace fs_front.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string permission);
    Task<string?> GetRoleAsync();
    Task<string?> GetUserIdAsync();
    Task<string?> GetUserEmailAsync();
    Task<int?> GetClientIdAsync();

    /// <summary>
    /// Resuelve todos los permisos en una sola operación desde los claims del JWT.
    /// Usar en OnInitializedAsync en lugar de llamar cada método por separado.
    /// </summary>
    Task<PermissionBundle> GetPermissionBundleAsync();
}