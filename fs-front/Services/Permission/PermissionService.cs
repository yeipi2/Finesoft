using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace fs_front.Services;

// DTO con todos los datos del usuario resueltos en una sola operación
public sealed class PermissionBundle
{
    public string? UserId { get; init; }
    public string? Role { get; init; }
    public string? Email { get; init; }
    public int? ClientId { get; init; }
    public bool CanView { get; init; }
    public bool CanCreate { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }

    public bool CanViewAll =>
        Role is "Admin" or "Administracion" or "Empleado" or "Supervisor";
}

public class PermissionService : IPermissionService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IUserApiService _userApi;

    // Caché en memoria (vive mientras el servicio esté en scope de sesión)
    private ClaimsPrincipal? _cachedUser;
    private int? _cachedClientId;
    private bool _clientIdResolved = false;

    public PermissionService(AuthenticationStateProvider authStateProvider, IUserApiService userApi)
    {
        _authStateProvider = authStateProvider;
        _userApi = userApi;

        // Invalidar caché cuando el usuario haga login/logout
        _authStateProvider.AuthenticationStateChanged += _ =>
        {
            _cachedUser = null;
            _cachedClientId = null;
            _clientIdResolved = false;
        };
    }

    // Una sola llamada a GetAuthenticationStateAsync por sesión
    private async ValueTask<ClaimsPrincipal> GetUserAsync()
    {
        if (_cachedUser is not null) return _cachedUser;
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        _cachedUser = authState.User;
        return _cachedUser;
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        var user = await GetUserAsync();
        return user.Claims.Any(c => c.Type == "permission" && c.Value == permission);
    }

    public async Task<string?> GetRoleAsync()
    {
        var user = await GetUserAsync();
        return user.FindFirst(ClaimTypes.Role)?.Value;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var user = await GetUserAsync();
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<string?> GetUserEmailAsync()
    {
        var user = await GetUserAsync();
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    public async Task<int?> GetClientIdAsync()
    {
        // Si ya lo resolvimos (aunque sea null), devolver directo sin HTTP
        if (_clientIdResolved) return _cachedClientId;

        var user = await GetUserAsync();
        var claim = user.FindFirst("clientId")?.Value;

        if (int.TryParse(claim, out int clientIdFromToken))
        {
            _cachedClientId = clientIdFromToken;
            _clientIdResolved = true;
            return _cachedClientId;
        }

        // Fallback: solo una petición HTTP por sesión
        try
        {
            var profile = await _userApi.GetMyProfileAsync();
            _cachedClientId = profile?.ClientId;
            _clientIdResolved = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PermissionService] Error al obtener perfil: {ex.Message}");
            _clientIdResolved = true; // no reintentar en cada llamada
        }

        return _cachedClientId;
    }

    /// <summary>
    /// Resuelve userId, role, email, clientId y permisos en una sola operación.
    /// Llama esto una vez en OnInitializedAsync y evita múltiples roundtrips.
    /// </summary>
    public async Task<PermissionBundle> GetPermissionBundleAsync()
    {
        var user = await GetUserAsync();
        var clientId = await GetClientIdAsync(); // usa caché después del primer await

        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(ClaimTypes.Email)?.Value;

        bool HasPerm(string p) =>
            user.Claims.Any(c => c.Type == "permission" && c.Value == p);

        return new PermissionBundle
        {
            UserId = userId,
            Role = string.IsNullOrEmpty(role) && clientId.HasValue ? "Cliente" : role,
            Email = email,
            ClientId = clientId,
            CanView = HasPerm("tickets.view"),
            CanCreate = HasPerm("tickets.create"),
            CanEdit = HasPerm("tickets.edit"),
            CanDelete = HasPerm("tickets.delete"),
        };
    }
}