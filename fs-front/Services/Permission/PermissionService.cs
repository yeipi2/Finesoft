using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using fs_front.DTO;

namespace fs_front.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string permission);
    Task<string?> GetRoleAsync();
    Task<string?> GetUserIdAsync();
    Task<string?> GetUserEmailAsync();
    Task<int?> GetClientIdAsync();
}

public class PermissionService : IPermissionService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IUserApiService _userApi;

    public PermissionService(AuthenticationStateProvider authStateProvider, IUserApiService userApi)
    {
        _authStateProvider = authStateProvider;
        _userApi = userApi;
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

    public async Task<string?> GetUserEmailAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(ClaimTypes.Email)?.Value;
    }

    public async Task<int?> GetClientIdAsync()
    {
        Console.WriteLine("[PermissionService] GetClientIdAsync iniciado");
        
        // Primero intentar obtener del token JWT
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var clientIdClaim = authState.User.FindFirst("clientId")?.Value;
        Console.WriteLine($"[PermissionService] clientId del token: {clientIdClaim}");
        
        if (int.TryParse(clientIdClaim, out int clientIdFromToken))
        {
            Console.WriteLine($"[PermissionService] ClientId desde token: {clientIdFromToken}");
            return clientIdFromToken;
        }

        // Si no está en el token, obtener del perfil del usuario
        try
        {
            Console.WriteLine("[PermissionService] Intentando obtener ClientId desde perfil...");
            var profile = await _userApi.GetMyProfileAsync();
            Console.WriteLine($"[PermissionService] Profile obtenido: Role={profile?.Role}, ClientId={profile?.ClientId}");
            return profile?.ClientId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PermissionService] Error al obtener perfil: {ex.Message}");
            return null;
        }
    }
}