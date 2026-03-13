using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
using System.IdentityModel.Tokens.Jwt;

namespace fs_front.Services;

public class NotificationsRealtimeClient
{
    private readonly HttpClient _http;
    private readonly ISyncLocalStorageService _ls;
    private HubConnection? _conn;

    public NotificationsRealtimeClient(HttpClient http, ISyncLocalStorageService ls)
    {
        _http = http;
        _ls = ls;
    }

    public bool IsConnected =>
        _conn?.State == HubConnectionState.Connected ||
        _conn?.State == HubConnectionState.Connecting ||
        _conn?.State == HubConnectionState.Reconnecting;

    private (string? userId, string? role) GetUserInfoFromToken()
    {
        // Primero intentar leer directamente del localStorage (más confiable)
        var userIdFromStorage = _ls.GetItem<string>("userId");
        var roleFromStorage = _ls.GetItem<string>("userRole");

        if (!string.IsNullOrEmpty(userIdFromStorage) || !string.IsNullOrEmpty(roleFromStorage))
        {
            Console.WriteLine($"[SignalR] Obtenido desde localStorage - UserId: {userIdFromStorage}, Role: {roleFromStorage}");
            return (userIdFromStorage, roleFromStorage);
        }

        // Fallback: intentar leer del token JWT
        try
        {
            var token = _ls.GetItem<string>("accessToken");
            if (string.IsNullOrEmpty(token)) return (null, null);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                      ?? jwt.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            var role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            Console.WriteLine($"[SignalR] Obtenido desde JWT - UserId: {userId}, Role: {role}");
            return (userId, role);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Error leyendo token: {ex.Message}");
            return (null, null);
        }
    }

    public async Task StartAsync()
    {
        if (_conn is not null)
        {
            return;
        }

        try
        {
            var token = _ls.GetItem<string>("accessToken");
            var hubUrl = new Uri(_http.BaseAddress!, "hubs/notifications");

            Console.WriteLine($"[SignalR] Conectando a {hubUrl}, Token presente: {!string.IsNullOrEmpty(token)}");

            _conn = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        options.Headers["Authorization"] = $"Bearer {token}";
                    }
                    options.AccessTokenProvider =
                        () => Task.FromResult(token);
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30) })
                .Build();

            _conn.On<NotificationItem>("ReceiveNotification", notification =>
            {
                Console.WriteLine($"[SignalR] Notificación recibida: {notification.Title}, ID: {notification.Id}");
                AppNotifications.AddNotification(notification, true);
            });

            _conn.Closed += async (error) =>
            {
                Console.WriteLine($"[SignalR] Conexión cerrada: {error?.Message}");
                await Task.Delay(5000);
            };

            _conn.Reconnecting += async (error) =>
            {
                Console.WriteLine($"[SignalR] Reconectando: {error?.Message}");
            };

            _conn.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"[SignalR] Reconectado: {connectionId}");
            };

            await _conn.StartAsync();
            Console.WriteLine($"[SignalR] Conexión iniciada. Estado: {_conn.State}");
            
            await JoinGroupsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] ERROR: {ex.Message}");
        }
    }

    public async Task JoinGroupsAsync()
    {
        if (_conn == null) return;

        // Reintentar varias veces con espera exponencial si el userId no está disponible
        var (userId, role) = GetUserInfoFromToken();
        var maxRetries = 5;
        var retryCount = 0;

        while (string.IsNullOrEmpty(userId) && retryCount < maxRetries)
        {
            retryCount++;
            Console.WriteLine($"[SignalR] userId vacío, reintento {retryCount}/{maxRetries}...");
            await Task.Delay(1000 * retryCount); // Espera exponencial: 1s, 2s, 3s, etc.
            (userId, role) = GetUserInfoFromToken();
        }

        Console.WriteLine($"[SignalR] UserId: {userId}, Role: {role}");

        if (!string.IsNullOrEmpty(userId))
        {
            try { 
                await _conn.InvokeAsync("JoinUserGroup", userId); 
                Console.WriteLine($"[SignalR] Joined user group: user-{userId}");
            } catch (Exception ex) { 
                Console.WriteLine($"[SignalR] Error joining user group: {ex.Message}"); 
            }
        }
        
        if (!string.IsNullOrEmpty(role))
        {
            Console.WriteLine($"[SignalR] Uniendo al grupo según rol: {role}");

            // Unirse siempre a "employees" si es Empleado o Supervisor
            if (role == "Empleado" || role == "Supervisor")
            {
                try {
                    await _conn.InvokeAsync("JoinEmployeesGroup");
                    Console.WriteLine($"[SignalR] ✅ Joined employees group");
                } catch (Exception ex) {
                    Console.WriteLine($"[SignalR] ❌ Error joining employees group: {ex.Message}");
                }
            }

            // Unirse a grupos de admin
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Administracion", StringComparison.OrdinalIgnoreCase))
            {
                try {
                    await _conn.InvokeAsync("JoinAdministracionGroup");
                    Console.WriteLine($"[SignalR] ✅ Joined administracion group");
                } catch (Exception ex) {
                    Console.WriteLine($"[SignalR] ❌ Error joining administracion group: {ex.Message}");
                }
                try {
                    await _conn.InvokeAsync("JoinAdminsGroup");
                    Console.WriteLine($"[SignalR] ✅ Joined admins group");
                } catch (Exception ex) {
                    Console.WriteLine($"[SignalR] ❌ Error joining admins group: {ex.Message}");
                }
            }

            // También unirse a "employees" si es Admin o Administracion (para recibir notificaciones de empleados)
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Administracion", StringComparison.OrdinalIgnoreCase))
            {
                try {
                    await _conn.InvokeAsync("JoinEmployeesGroup");
                    Console.WriteLine($"[SignalR] ✅ Admin/Adminacion joined employees group (to receive all)");
                } catch (Exception ex) {
                    Console.WriteLine($"[SignalR] Error joining employees group for admin: {ex.Message}");
                }
            }
        }
    }

    public async Task StopAsync()
    {
        if (_conn is null) return;
        await _conn.StopAsync();
        await _conn.DisposeAsync();
        _conn = null;
    }
}
