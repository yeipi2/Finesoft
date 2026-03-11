using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using fs_backend.Identity;

namespace fs_backend.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, int entityId, string? oldValue = null, string? newValue = null);
    Task LogLoginAsync(string userId, string userName, bool success, string? failureReason = null);
    Task LogAccessDeniedAsync(string userId, string resource, string reason);
    Task<IEnumerable<AuditLogDto>> GetLogsAsync(DateTime? from = null, DateTime? to = null, string? userId = null, string? action = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityName, int entityId, string? oldValue = null, string? newValue = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId.ToString(),
                OldValue = TruncateValue(oldValue, 4000),
                NewValue = TruncateValue(newValue, 4000),
                IpAddress = TruncateValue(ipAddress, 45),
                UserAgent = TruncateValue(userAgent, 500),
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "AUDIT: {Action} on {EntityName}:{EntityId} by {UserId}",
                action, entityName, entityId, userId ?? "Anonymous");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar audit log para {Action}", action);
        }
    }

    public async Task LogLoginAsync(string userId, string userName, bool success, string? failureReason = null)
    {
        var action = success ? "USER_LOGIN_SUCCESS" : "USER_LOGIN_FAILED";
        
        await LogAsync(
            action,
            "User",
            0,
            newValue: $"{userName} - {(success ? "Login exitoso" : $"Fallido: {failureReason}")}"
        );
    }

    public async Task LogAccessDeniedAsync(string userId, string resource, string reason)
    {
        await LogAsync(
            "ACCESS_DENIED",
            "Security",
            0,
            newValue: $"User {userId} denied access to {resource}: {reason}"
        );
    }

    public async Task<IEnumerable<AuditLogDto>> GetLogsAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? userId = null,
        string? action = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Timestamp <= to.Value);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(x => x.UserId == userId);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(x => x.Action == action);

        return await query
            .OrderByDescending(x => x.Timestamp)
            .Take(1000)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                IpAddress = x.IpAddress,
                Timestamp = x.Timestamp
            })
            .ToListAsync();
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return "Unknown";

        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
            ip = context.Connection.RemoteIpAddress?.ToString();

        return ip ?? "Unknown";
    }

    private string GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }

    private static string? TruncateValue(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }
}

public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
