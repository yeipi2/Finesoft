using Microsoft.AspNetCore.SignalR;

namespace fs_backend.Hubs;

/// <summary>
/// Hub dedicado para actualizaciones en tiempo real de cotizaciones.
/// No requiere autenticación para que el cliente público también pueda
/// disparar eventos (el endpoint del controller lo invoca directamente).
/// </summary>
public class QuotesHub : Hub
{
    // Los clientes internos (usuarios logueados) se unen al grupo "internal-users"
    // desde el frontend al conectarse.
    public async Task JoinInternalGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "internal-users");
    }

    public async Task LeaveInternalGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "internal-users");
    }
}