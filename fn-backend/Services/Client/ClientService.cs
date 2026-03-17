using fn_backend.DTO;
using fn_backend.Models;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<ClientService> _logger;
    private readonly ICacheService _cache;
    private readonly INotificationHelper _notificationHelper;

    public ClientService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ILogger<ClientService> logger,
        ICacheService cache,
        INotificationHelper notificationHelper)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _cache = cache;
        _notificationHelper = notificationHelper;
    }

    public async Task<ServiceResult<Client>> CreateClientAsync(ClientDto dto)
    {
        // ✅ 1. Crear el usuario primero
        if (string.IsNullOrEmpty(dto.Password))
        {
            return ServiceResult<Client>.Failure(
                "La contraseña es requerida para crear un cliente");
        }

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var userResult = await _userManager.CreateAsync(user, dto.Password);
        if (!userResult.Succeeded)
        {
            return ServiceResult<Client>.Failure(
                userResult.Errors.Select(e => e.Description));
        }

        // ✅ 2. Asignar rol "Cliente"
        var roleResult = await _userManager.AddToRoleAsync(user, "Cliente");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ServiceResult<Client>.Failure(
                roleResult.Errors.Select(e => e.Description));
        }

        // ✅ 3. Crear el cliente vinculado al usuario
        var client = new Client
        {
            UserId = user.Id,
            CompanyName = dto.CompanyName,
            ContactName = dto.ContactName,
            Email = dto.Email,
            Phone = dto.Phone,
            RFC = dto.RFC,
            Address = dto.Address,
            ServiceMode = dto.ServiceMode,
            BillingFrequency = dto.ServiceMode == "Mensual" ? "Monthly" : "Event", // ⭐ AGREGAR
            MonthlyRate = dto.MonthlyRate,
            IsActive = dto.IsActive,
            MonthlyHours = dto.MonthlyHours,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Notificación de cliente creado
            var clientNotification = _notificationHelper.CreateNotification(
                NotificationType.ClientCreated,
                "Nuevo Cliente Creado",
                $"Se ha creado el cliente {client.CompanyName}",
                $"/clients/{client.Id}");
            await _notificationHelper.SendToAdminsAsync(clientNotification);
            await _notificationHelper.SendToAdministracionAsync(clientNotification);

            _logger.LogInformation(
                "✅ Cliente creado: {CompanyName} (User: {UserId})",
                client.CompanyName, user.Id);

            // Invalidar cache de clientes
            await _cache.InvalidateAsync(CacheKeys.AllClients);

            return ServiceResult<Client>.Success(client);
        }
        catch (Exception ex)
        {
            // Rollback: eliminar usuario si falla la creación del cliente
            await _userManager.DeleteAsync(user);
            _logger.LogError(ex, "❌ Error al crear cliente");
            return ServiceResult<Client>.Failure(
                $"Error al guardar el cliente: {ex.Message}");
        }
    }

    // ClientService.cs — GetClientsAsync
    public async Task<IEnumerable<ClientDto>> GetClientsAsync()
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.AllClients,
            async () =>
            {
                var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var nextMonth = currentMonth.AddMonths(1);

                var clients = await _context.Clients
                    .Include(c => c.Projects)
                    .ToListAsync();

                var result = new List<ClientDto>();

                foreach (var c in clients)
                {
                    // Sumar ActualHours de tickets del mes actual para este cliente
                    var projectIds = c.Projects?.Select(p => p.Id).ToList() ?? new List<int>();

                    decimal hoursUsed = 0;
                    if (projectIds.Any())
                    {
                        hoursUsed = await _context.Tickets
                            .Where(t => t.ProjectId.HasValue
                                     && projectIds.Contains(t.ProjectId.Value)
                                     && t.UpdatedAt >= currentMonth
                                     && t.UpdatedAt < nextMonth)
                            .SumAsync(t => t.ActualHours);
                    }

                    result.Add(new ClientDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        CompanyName = c.CompanyName,
                        ContactName = c.ContactName,
                        Email = c.Email,
                        Phone = c.Phone,
                        RFC = c.RFC,
                        Address = c.Address,
                        ServiceMode = c.ServiceMode,
                        MonthlyRate = c.MonthlyRate,
                        MonthlyHours = c.MonthlyHours,
                        MonthlyHoursUsed = hoursUsed,
                        IsActive = c.IsActive,
                        ProjectCount = c.Projects?.Count ?? 0
                    });
                }

                return result;
            },
            TimeSpan.FromMinutes(15) // Cache de 15 minutos
        ) ?? new List<ClientDto>();
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        var cacheKey = string.Format(CacheKeys.ClientById, id);

        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _context.Clients.FindAsync(id),
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task<ServiceResult<bool>> UpdateClientAsync(int id, ClientDto dto)
    {
        try
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return ServiceResult<bool>.Failure("Cliente no encontrado");
            }

            client.CompanyName = dto.CompanyName;
            client.ContactName = dto.ContactName;
            client.Email = dto.Email;
            client.Phone = dto.Phone;
            client.RFC = dto.RFC;
            client.Address = dto.Address;
            client.ServiceMode = dto.ServiceMode;
            client.BillingFrequency = dto.ServiceMode == "Mensual" ? "Monthly" : "Event";
            client.MonthlyRate = dto.MonthlyRate;
            client.IsActive = dto.IsActive;
            client.MonthlyHours = dto.MonthlyHours;
            client.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(client.UserId))
            {
                var user = await _userManager.FindByIdAsync(client.UserId);
                if (user != null)
                {
                    user.LockoutEnabled = !client.IsActive;
                    user.LockoutEnd = client.IsActive ? null : DateTimeOffset.MaxValue;
                    await _userManager.UpdateAsync(user);
                }
            }

            _context.Clients.Update(client);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Cliente actualizado: {Id}", id);

            await _cache.InvalidateAsync(CacheKeys.AllClients);
            await _cache.InvalidateAsync(string.Format(CacheKeys.ClientById, id));

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al actualizar cliente {Id}", id);
            return ServiceResult<bool>.Failure($"Error al actualizar el cliente: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteClientAsync(int id)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.Projects)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return ServiceResult<bool>.Failure("Cliente no encontrado");

            client.IsActive = !client.IsActive;
            client.UpdatedAt = DateTime.UtcNow;

            if (client.Projects != null && client.Projects.Any())
            {
                foreach (var project in client.Projects)
                {
                    project.IsActive = client.IsActive;
                }
                _logger.LogInformation(
                    "✅ {Count} proyectos {Status} para cliente {Id}",
                    client.Projects.Count,
                    client.IsActive ? "reactivados" : "desactivados",
                    id);
            }

            if (!string.IsNullOrEmpty(client.UserId))
            {
                var user = await _userManager.FindByIdAsync(client.UserId);
                if (user != null)
                {
                    if (client.IsActive)
                    {
                        user.LockoutEnabled = false;
                        user.LockoutEnd = null;
                    }
                    else
                    {
                        user.LockoutEnabled = true;
                        user.LockoutEnd = DateTimeOffset.MaxValue;
                    }
                    await _userManager.UpdateAsync(user);
                }
            }

            await _context.SaveChangesAsync();

            await _cache.InvalidateAsync(CacheKeys.AllClients);
            await _cache.InvalidateAsync(string.Format(CacheKeys.ClientById, id));

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al eliminar cliente {Id}", id);
            return ServiceResult<bool>.Failure($"Error al eliminar el cliente: {ex.Message}");
        }
    }


    public async Task<List<ClientDto>> SearchClientsAsync(string query)
    {
        // El search no se cachea por ser dinámico
        var clients = await _context.Clients
            .Where(c => c.IsActive &&
                       (c.CompanyName.Contains(query) ||
                        c.ContactName.Contains(query) ||
                        c.Email.Contains(query) ||
                        c.RFC.Contains(query)))
            .OrderBy(c => c.CompanyName)
            .Take(10)
            .ToListAsync();

        return clients.Select(c => new ClientDto
        {
            Id = c.Id,
            UserId = c.UserId,
            CompanyName = c.CompanyName,
            ContactName = c.ContactName,
            Email = c.Email,
            Phone = c.Phone,
            RFC = c.RFC,
            Address = c.Address,
            ServiceMode = c.ServiceMode,
            MonthlyRate = c.MonthlyRate,
            IsActive = c.IsActive
        }).ToList();
    }
}
