using Asp.Versioning;
using fs_backend.DTO;
using fs_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fs_backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// GET: api/notifications
    /// Obtiene todas las notificaciones del usuario actual
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
        return Ok(notifications);
    }

    /// <summary>
    /// GET: api/notifications/unread-count
    /// Obtiene el conteo de notificaciones no leídas
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// POST: api/notifications/{id}/read
    /// Marca una notificación como leída
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _notificationService.MarkAsReadAsync(userId, id);
        return Ok(new { message = "Notificación marcada como leída" });
    }

    /// <summary>
    /// POST: api/notifications/read-all
    /// Marca todas las notificaciones como leídas
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { message = "Todas las notificaciones marcadas como leídas" });
    }

    /// <summary>
    /// DELETE: api/notifications
    /// Elimina todas las notificaciones del usuario
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAll()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _notificationService.DeleteAllAsync(userId);
        return Ok(new { message = "Todas las notificaciones eliminadas" });
    }
}