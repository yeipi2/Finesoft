using fn_backend.Models;
using fs_backend.DTO;
using fs_backend.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public interface INotificationService
{
    Task<int> SaveNotificationAsync(string userId, NotificationDto notification);
    Task SaveNotificationsAsync(IEnumerable<(string UserId, NotificationDto Notification)> notifications);
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(string userId, int notificationId);
    Task MarkAllAsReadAsync(string userId);
    Task DeleteOldNotificationsAsync(int daysToKeep = 30);
    Task DeleteAllAsync(string userId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveNotificationAsync(string userId, NotificationDto notification)
    {
        var notificationEntity = new Notification
        {
            UserId = userId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            Link = notification.Link,
            IconClass = notification.IconClass,
            IconColor = notification.IconColor,
            IsRead = false,
            CreatedAt = notification.CreatedAt == default ? DateTime.UtcNow : notification.CreatedAt,
            TargetRole = notification.TargetRole,
            Severity = notification.Severity
        };

        _context.Notifications.Add(notificationEntity);
        await _context.SaveChangesAsync();

        notification.Id = notificationEntity.Id.ToString();
        return notificationEntity.Id;
    }

    public async Task SaveNotificationsAsync(IEnumerable<(string UserId, NotificationDto Notification)> notifications)
    {
        var entities = notifications.Select(n => new Notification
        {
            UserId = n.UserId,
            Type = n.Notification.Type,
            Title = n.Notification.Title,
            Message = n.Notification.Message,
            Link = n.Notification.Link,
            IconClass = n.Notification.IconClass,
            IconColor = n.Notification.IconColor,
            IsRead = false,
            CreatedAt = n.Notification.CreatedAt == default ? DateTime.UtcNow : n.Notification.CreatedAt,
            TargetRole = n.Notification.TargetRole,
            Severity = n.Notification.Severity
        });

        await _context.Notifications.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id.ToString(),
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            Link = n.Link,
            IconClass = n.IconClass,
            IconColor = n.IconColor,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            TargetRole = n.TargetRole,
            Severity = n.Severity
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(string userId, int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteOldNotificationsAsync(int daysToKeep = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldNotifications = await _context.Notifications
            .Where(n => n.IsRead && n.CreatedAt < cutoffDate)
            .ToListAsync();

        _context.Notifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAllAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync();
    }
}