using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;

namespace fs_front.Services;

public class PermissionsRealtimeClient
{
    private readonly HttpClient _http;
    private readonly ISyncLocalStorageService _ls;
    private HubConnection? _conn;

    public event Func<Task>? PermissionsChanged;

    public PermissionsRealtimeClient(HttpClient http, ISyncLocalStorageService ls)
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

        // ✅ usar backend base address
        var hubUrl = new Uri(_http.BaseAddress!, "hubs/permissions");

        _conn = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider =
                    () => Task.FromResult(_ls.GetItem<string>("accessToken"));
            })
            .WithAutomaticReconnect()
            .Build();

        _conn.On<object>("PermissionsChanged", async _ =>
        {
            if (PermissionsChanged is not null)
                await PermissionsChanged.Invoke();
        });

        await _conn.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_conn is null) return;
        await _conn.StopAsync();
        await _conn.DisposeAsync();
        _conn = null;
    }
}
