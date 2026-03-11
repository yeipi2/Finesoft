// ============================================
// CACHE SERVICE - fn-backend/Services/CacheService.cs
// Sistema de cache en memoria con invalidación automática
// ============================================

using Microsoft.Extensions.Caching.Memory;

namespace fs_backend.Services;

public interface ICacheService
{
    // Cache sin fecha de expiración (solo se invalida manualmente)
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task InvalidateAsync(string key);
    Task InvalidatePatternAsync(string pattern);

    // Métodos de conveniencia para cache de listas
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache MISS: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var options = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }
        else
        {
            // Por defecto, cache de 30 minutos
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        }

        options.SetSlidingExpiration(TimeSpan.FromMinutes(10));

        _cache.Set(key, value, options);
        _logger.LogDebug("Cache SET: {Key}", key);

        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache INVALIDATED: {Key}", key);
        return Task.CompletedTask;
    }

    public Task InvalidatePatternAsync(string pattern)
    {
        // IMemoryCache no soporta búsqueda por patrón
        // Para eso se necesita un cache distribuido como Redis
        // Aquí simplemente warnamos
        _logger.LogWarning("Pattern invalidation not supported in MemoryCache. Key: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, expiration);
        }

        return value;
    }
}

// ============================================
// KEYS DE CACHE - Constantes para evitar errores de typing
// ============================================
public static class CacheKeys
{
    // Permisos (cache largo - 1 hora)
    public const string AllPermissions = "permissions:all";
    public const string RolePermissions = "permissions:role:{0}"; // {0} = roleId

    // Clientes (cache medio - 15 minutos)
    public const string AllClients = "clients:all";
    public const string ClientById = "clients:id:{0}";        // {0} = clientId
    public const string ClientSearch = "clients:search:{0}"; // {0} = query

    // Empleados (cache medio - 15 minutos)
    public const string AllEmployees = "employees:all";
    public const string EmployeeById = "employees:id:{0}";    // {0} = employeeId

    // Proyectos (cache medio - 15 minutos)
    public const string AllProjects = "projects:all";
    public const string ProjectById = "projects:id:{0}";      // {0} = projectId

    // Configuraciones (cache largo)
    public const string AllTypeServices = "typeservices:all";
    public const string AllTypeActivities = "typeactivities:all";

    // Stats (cache corto - 5 minutos)
    public const string TicketStats = "tickets:stats:{0}:{1}"; // {0} = userId, {1} = byCreator
}
