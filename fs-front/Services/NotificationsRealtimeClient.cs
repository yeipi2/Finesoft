using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;

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

    public async Task StartAsync()
    {
        if (_conn is not null) return;

        var hubUrl = new Uri(_http.BaseAddress!, "hubs/notifications");

        _conn = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider =
                    () => Task.FromResult(_ls.GetItem<string>("accessToken"));
            })
            .WithAutomaticReconnect()
            .Build();

        _conn.On<NotificationItem>("ReceiveNotification", notification =>
        {
            AppNotifications.AddNotification(notification);
        });

        // Unirse a grupos según el rol
        var role = _ls.GetItem<string>("userRole");
        if (!string.IsNullOrEmpty(role))
        {
            _conn.On("ReceiveNotification", async () =>
            {
                // El servidor envía a grupos, aquí solo necesitamos escuchar
            });
        }

        await _conn.StartAsync();
        
        // Unirse al grupo del usuario específico
        var userId = _ls.GetItem<string>("userId");
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                await _conn.InvokeAsync("JoinUserGroup", userId);
            }
            catch { }
        }
        
        // Unirse al grupo de empleados si es empleado
        if (role == "Empleado" || role == "Supervisor")
        {
            try
            {
                await _conn.InvokeAsync("JoinEmployeesGroup");
            }
            catch { }
        }

        // Unirse al grupo de administración si es admin o administración
        if (role == "Admin" || role == "Administracion")
        {
            try
            {
                await _conn.InvokeAsync("JoinAdministracionGroup");
            }
            catch { }
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
