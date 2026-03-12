namespace fs_front.Services;

public class NotificationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string IconClass { get; set; } = "bi bi-bell";
    public string IconColor { get; set; } = "#6B46C1";
}

public static class AppNotifications
{
    public static event Action<NotificationItem>? OnNotificationReceived;
    public static event Action? OnNotificationsUpdated;

    private static readonly List<NotificationItem> _notifications = new();
    private static readonly object _lock = new();

    public static IReadOnlyList<NotificationItem> Notifications
    {
        get
        {
            lock (_lock)
            {
                return _notifications.OrderByDescending(n => n.CreatedAt).ToList();
            }
        }
    }

    public static int UnreadCount
    {
        get
        {
            lock (_lock)
            {
                return _notifications.Count(n => !n.IsRead);
            }
        }
    }

    public static void AddNotification(NotificationItem notification)
    {
        lock (_lock)
        {
            _notifications.Insert(0, notification);
            
            // Limitar a 50 notificaciones máximo
            if (_notifications.Count > 50)
            {
                _notifications.RemoveAt(_notifications.Count - 1);
            }
        }
        
        OnNotificationReceived?.Invoke(notification);
        OnNotificationsUpdated?.Invoke();
    }

    public static void MarkAsRead(string notificationId)
    {
        lock (_lock)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                OnNotificationsUpdated?.Invoke();
            }
        }
    }

    public static void MarkAllAsRead()
    {
        lock (_lock)
        {
            foreach (var notification in _notifications)
            {
                notification.IsRead = true;
            }
        }
        OnNotificationsUpdated?.Invoke();
    }

    public static void ClearAll()
    {
        lock (_lock)
        {
            _notifications.Clear();
        }
        OnNotificationsUpdated?.Invoke();
    }
}
