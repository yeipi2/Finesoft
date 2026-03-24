using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using fn_backend.Models;
using fs_backend.DTO;
using fs_backend.Hubs;
using fs_backend.Identity;

namespace fs_backend.Services;

public interface INotificationHelper
{
    /// <summary>
    /// Crea una notificación con valores por defecto según el tipo
    /// </summary>
    NotificationDto CreateNotification(NotificationType type, string title, string message, string? link = null);

    /// <summary>
    /// Crea una notificación incluyendo el nombre de quien la creó
    /// </summary>
    Task<NotificationDto> CreateNotificationWithCreatorAsync(NotificationType type, string title, string message, string creatorUserId, string? link = null);

    /// <summary>
    /// Envía notificación a un usuario específico (BD + SignalR)
    /// </summary>
    Task SendToUserAsync(string userId, NotificationDto notification);

    /// <summary>
    /// Envía notificación a múltiples usuarios (BD + SignalR)
    /// </summary>
    /// <param name="userIds">Lista de usuarios a notificar</param>
    /// <param name="notification">Notificación a enviar</param>
    /// <param name="excludeUserId">ID de usuario a excluir de la notificación (ej: el creador de la acción)</param>
    Task SendToUsersAsync(IEnumerable<string> userIds, NotificationDto notification, string? excludeUserId = null);

    /// <summary>
    /// Envía notificación a un rol específico (BD + SignalR)
    /// </summary>
    Task SendToRoleAsync(string role, NotificationDto notification, string? excludeUserId = null);

    /// <summary>
    /// Envía notificación a múltiples roles (BD + SignalR)
    /// </summary>
    Task SendToRolesAsync(IEnumerable<string> roles, NotificationDto notification, string? excludeUserId = null);

    /// <summary>
    /// Envía notificación a administradores (BD + SignalR)
    /// </summary>
    /// <param name="notification">Notificación a enviar</param>
    /// <param name="excludeUserId">ID de usuario a excluir de la notificación (ej: el creador de la acción)</param>
    Task SendToAdminsAsync(NotificationDto notification, string? excludeUserId = null);

    /// <summary>
    /// Envía notificación a administración (BD + SignalR)
    /// </summary>
    Task SendToAdministracionAsync(NotificationDto notification, string? excludeUserId = null);

    /// <summary>
    /// Envía notificación a empleados (BD + SignalR)
    /// </summary>
    Task SendToEmployeesAsync(NotificationDto notification, string? excludeUserId = null);

    /// <summary>
    /// Obtiene los IDs de usuarios por rol
    /// </summary>
    Task<List<string>> GetUserIdsByRoleAsync(string role);

    /// <summary>
    /// Obtiene los IDs de usuarios por múltiples roles
    /// </summary>
    Task<List<string>> GetUserIdsByRolesAsync(IEnumerable<string> roles);
}

public class NotificationHelper : INotificationHelper
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificationHelper(
        INotificationService notificationService,
        IHubContext<NotificationsHub> hubContext,
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _context = context;
        _userManager = userManager;
    }

    public NotificationDto CreateNotification(NotificationType type, string title, string message, string? link = null)
    {
        var defaults = NotificationDefaults.Defaults.GetValueOrDefault(type);
        var notification = new NotificationDto
        {
            Type = type.ToString(),
            Title = title,
            Message = message,
            Link = link,
            IconClass = defaults.IconClass,
            IconColor = defaults.IconColor,
            Severity = defaults.Severity
        };

        return notification;
    }

    public async Task<NotificationDto> CreateNotificationWithCreatorAsync(NotificationType type, string title, string message, string creatorUserId, string? link = null)
    {
        var (role, name) = await GetUserRoleAndNameAsync(creatorUserId);
        var creatorInfo = string.IsNullOrEmpty(name) 
            ? (string.IsNullOrEmpty(role) ? "" : role)
            : (string.IsNullOrEmpty(role) ? name : $"{role}: {name}");
        
        var fullMessage = string.IsNullOrEmpty(creatorInfo) 
            ? message 
            : $"{message} ({creatorInfo})";
        
        return CreateNotification(type, title, fullMessage, link);
    }

    private async Task<(string? Role, string? Name)> GetUserRoleAndNameAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return (null, null);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (null, null);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        // Buscar nombre en empleados
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee != null && !string.IsNullOrEmpty(employee.FullName))
        {
            return (role, employee.FullName);
        }

        // Buscar nombre en clientes
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client != null && !string.IsNullOrEmpty(client.ContactName))
        {
            return (role, client.ContactName);
        }

        // Si no tiene nombre, usar email sin dominio
        var email = user.Email ?? user.UserName;
        var shortEmail = email?.Split('@')[0];
        return (role, shortEmail);
    }

    private async Task<string?> GetUserDisplayNameAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        // Primero siempre buscar en empleados (prioridad: nombre completo)
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee != null && !string.IsNullOrEmpty(employee.FullName))
        {
            return employee.FullName;
        }

        // Si no es empleado, buscar el usuario
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        // Intentar obtener nombre del cliente
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client != null && !string.IsNullOrEmpty(client.ContactName))
        {
            return client.ContactName;
        }

        // Si no tiene nombre en ninguna tabla, mostrar el email sin dominio
        var email = user.Email ?? user.UserName;
        return email?.Split('@')[0];
    }

    public async Task SendToUserAsync(string userId, NotificationDto notification)
    {
        // Guardar en BD
        await _notificationService.SaveNotificationAsync(userId, notification);
        // Enviar por SignalR
        await NotificationsHub.SendToUser(_hubContext, userId, notification);
    }

    public async Task SendToUsersAsync(IEnumerable<string> userIds, NotificationDto notification, string? excludeUserId = null)
    {
        var userIdList = userIds.ToList();
        if (!userIdList.Any()) return;

        // Excluir el usuario especificado
        if (!string.IsNullOrEmpty(excludeUserId))
        {
            userIdList = userIdList.Where(id => id != excludeUserId).ToList();
        }

        if (!userIdList.Any()) return;

        // Guardar en BD para todos los usuarios
        var notifications = userIdList.Select(userId => (userId, notification)).ToList();
        await _notificationService.SaveNotificationsAsync(notifications);

        // Enviar por SignalR a cada usuario
        foreach (var userId in userIdList)
        {
            await NotificationsHub.SendToUser(_hubContext, userId, notification);
        }
    }

    public async Task SendToRoleAsync(string role, NotificationDto notification, string? excludeUserId = null)
    {
        var userIds = await GetUserIdsByRoleAsync(role);
        await SendToUsersAsync(userIds, notification, excludeUserId);

        // También enviar por el grupo de SignalR (a todos en el rol, la exclusión es a nivel de notificación guardada)
        await NotificationsHub.SendToRole(_hubContext, role.ToLower(), notification);
    }

    public async Task SendToRolesAsync(IEnumerable<string> roles, NotificationDto notification, string? excludeUserId = null)
    {
        foreach (var role in roles)
        {
            await SendToRoleAsync(role, notification, excludeUserId);
        }
    }

    public async Task SendToAdminsAsync(NotificationDto notification, string? excludeUserId = null)
    {
        // Obtener todos los usuarios de Admin y Administracion para evitar duplicados
        var allAdminIds = await GetUserIdsByRolesAsync(new[] { "Admin", "Administracion" });
        var uniqueIds = allAdminIds.Distinct().ToList();
        await SendToUsersAsync(uniqueIds, notification, excludeUserId);
    }

    public async Task SendToAdministracionAsync(NotificationDto notification, string? excludeUserId = null)
    {
        // Ya no enviar por separado para evitar duplicados con Admin
        // SendToAdminsAsync ya incluye Administracion
        // Pero si se llama单独, enviar solo a Administracion
        var adminIds = await GetUserIdsByRoleAsync("Administracion");
        var adminSet = new HashSet<string>(adminIds);

        // Excluir usuarios que ya recibieron como Admin
        var adminUserIds = await GetUserIdsByRoleAsync("Admin");
        var finalIds = adminIds.Where(id => !adminSet.Contains(id)).ToList();

        if (finalIds.Any())
        {
            await SendToUsersAsync(finalIds, notification, excludeUserId);
        }
    }

    public async Task SendToEmployeesAsync(NotificationDto notification, string? excludeUserId = null)
    {
        var employeeIds = await GetUserIdsByRoleAsync("Empleado");
        await SendToUsersAsync(employeeIds, notification, excludeUserId);
    }

    public async Task<List<string>> GetUserIdsByRoleAsync(string role)
    {
        var userRoles = await _context.UserRoles
            .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role))
            .Select(ur => ur.UserId)
            .ToListAsync();

        return userRoles;
    }

    public async Task<List<string>> GetUserIdsByRolesAsync(IEnumerable<string> roles)
    {
        var roleList = roles.ToList();
        var userRoles = await _context.UserRoles
            .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && roleList.Contains(r.Name)))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        return userRoles;
    }
}

// Extensión para obtener valores del diccionario
public static class NotificationDefaultsExtensions
{
    public static (string IconClass, string IconColor, string Severity) GetNotificationDefaults(this NotificationType type)
    {
        return NotificationDefaults.Defaults.GetValueOrDefault(type);
    }
}