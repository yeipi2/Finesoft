using System.Net.Http.Json;

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
    private static bool _isLoaded = false;
    private static HttpClient? _http;

    public static void Initialize(HttpClient http)
    {
        _http = http;
    }

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

    public static void AddNotification(NotificationItem notification, bool isFromSignalR = false)
    {
        Console.WriteLine($"[AppNotifications] AddNotification: {notification.Title}, isFromSignalR: {isFromSignalR}");
        
        lock (_lock)
        {
            _notifications.Insert(0, notification);

            if (_notifications.Count > 50)
            {
                _notifications.RemoveAt(_notifications.Count - 1);
            }
        }

        OnNotificationReceived?.Invoke(notification);
        OnNotificationsUpdated?.Invoke();
    }

    public static async Task LoadFromApiAsync(HttpClient http)
    {
        _http = http;
        if (_isLoaded) return;

        try
        {
            var response = await http.GetAsync("api/notifications");
            if (response.IsSuccessStatusCode)
            {
                var notifications = await response.Content.ReadFromJsonAsync<List<NotificationItem>>();
                if (notifications != null && notifications.Count > 0)
                {
                    lock (_lock)
                    {
                        foreach (var notif in notifications)
                        {
                            if (!_notifications.Any(n => n.Id == notif.Id))
                            {
                                _notifications.Add(notif);
                            }
                        }
                    }
                    _isLoaded = true;
                    OnNotificationsUpdated?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppNotifications] Error loading from API: {ex.Message}");
        }
    }

    public static async Task MarkAsRead(string notificationId)
    {
        NotificationItem? notification;
        lock (_lock)
        {
            notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
        }
        OnNotificationsUpdated?.Invoke();

        if (_http != null && notification != null)
        {
            try
            {
                await _http.PostAsync($"api/notifications/{notificationId}/read", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppNotifications] Error marking as read: {ex.Message}");
            }
        }
    }

    public static async Task MarkAllAsRead()
    {
        List<NotificationItem> allNotifications;
        lock (_lock)
        {
            allNotifications = _notifications.ToList();
            foreach (var notification in allNotifications)
            {
                notification.IsRead = true;
            }
        }
        OnNotificationsUpdated?.Invoke();

        if (_http != null)
        {
            try
            {
                await _http.PostAsync("api/notifications/read-all", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppNotifications] Error marking all as read: {ex.Message}");
            }
        }
    }

    public static async Task ClearAll()
    {
        lock (_lock)
        {
            _notifications.Clear();
            _isLoaded = false;
        }
        OnNotificationsUpdated?.Invoke();

        if (_http != null)
        {
            try
            {
                await _http.DeleteAsync("api/notifications");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppNotifications] Error clearing notifications: {ex.Message}");
            }
        }
    }
}
