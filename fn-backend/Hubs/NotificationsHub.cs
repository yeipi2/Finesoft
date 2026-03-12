using Microsoft.AspNetCore.SignalR;
using fs_backend.DTO;

namespace fs_backend.Hubs;

public class NotificationsHub : Hub
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public NotificationsHub(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    public async Task JoinRoleGroup(string role)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");
    }

    public async Task LeaveRoleGroup(string role)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role-{role}");
    }

    public async Task JoinAdminsGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }

    public async Task JoinEmployeesGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "employees");
    }

    public async Task JoinAdministracionGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "administracion");
    }

    public static async Task SendToUser(IHubContext<NotificationsHub> hub, string userId, NotificationDto notification)
    {
        await hub.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendToRole(IHubContext<NotificationsHub> hub, string role, NotificationDto notification)
    {
        await hub.Clients.Group($"role-{role}").SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendToAdmins(IHubContext<NotificationsHub> hub, NotificationDto notification)
    {
        await hub.Clients.Group("admins").SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendToEmployees(IHubContext<NotificationsHub> hub, NotificationDto notification)
    {
        await hub.Clients.Group("employees").SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendToAdministracion(IHubContext<NotificationsHub> hub, NotificationDto notification)
    {
        await hub.Clients.Group("administracion").SendAsync("ReceiveNotification", notification);
    }

    public static async Task SendToAll(IHubContext<NotificationsHub> hub, NotificationDto notification)
    {
        await hub.Clients.All.SendAsync("ReceiveNotification", notification);
    }
}
