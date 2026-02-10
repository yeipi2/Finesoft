using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace fs_front.Services;

public class PermissionService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public PermissionService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    // Verifica si el usuario tiene un permiso específico (claim "permission")
    public async Task<bool> HasPermissionAsync(string permission)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Claims
            .Any(c => c.Type == "permission" && c.Value == permission);
    }

    public async Task<string?> GetRoleAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(ClaimTypes.Role)?.Value;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}