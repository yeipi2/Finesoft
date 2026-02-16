using fn_backend.DTO;
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

    public ClientService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ILogger<ClientService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
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
            MonthlyRate = dto.MonthlyRate,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Cliente creado: {CompanyName} (User: {UserId})",
                client.CompanyName, user.Id);

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

    public async Task<IEnumerable<Client>> GetClientsAsync()
    {
        var clients = await _context.Clients.ToListAsync();
        return clients;
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        return client;
    }

    public async Task<bool> UpdateClientAsync(int id, ClientDto dto)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return false;
        }

        // ✅ Actualizar datos del cliente
        client.CompanyName = dto.CompanyName;
        client.ContactName = dto.ContactName;
        client.Email = dto.Email;
        client.Phone = dto.Phone;
        client.RFC = dto.RFC;
        client.Address = dto.Address;
        client.ServiceMode = dto.ServiceMode;
        client.MonthlyRate = dto.MonthlyRate;
        client.IsActive = dto.IsActive;
        client.UpdatedAt = DateTime.UtcNow;

        // ✅ Actualizar email del usuario si existe
        if (!string.IsNullOrEmpty(client.UserId))
        {
            var user = await _userManager.FindByIdAsync(client.UserId);
            if (user != null)
            {
                user.Email = dto.Email;
                user.UserName = dto.Email;
                await _userManager.UpdateAsync(user);
            }
        }

        _context.Clients.Update(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Cliente actualizado: {Id}", id);
        return true;
    }

    public async Task<bool> DeleteClientAsync(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return false;

        // ⭐ CAMBIO: Alternar estado en lugar de solo desactivar
        client.IsActive = !client.IsActive;
        client.UpdatedAt = DateTime.UtcNow;

        // ⭐ Actualizar usuario según el nuevo estado
        if (!string.IsNullOrEmpty(client.UserId))
        {
            var user = await _userManager.FindByIdAsync(client.UserId);
            if (user != null)
            {
                if (client.IsActive)
                {
                    // ✅ Reactivar: quitar bloqueo
                    user.LockoutEnabled = false;
                    user.LockoutEnd = null;
                    _logger.LogInformation("✅ Cliente reactivado: {Id}", id);
                }
                else
                {
                    // ❌ Desactivar: bloquear acceso
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue;
                    _logger.LogInformation("✅ Cliente desactivado: {Id}", id);
                }

                await _userManager.UpdateAsync(user);
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }


    public async Task<List<ClientDto>> SearchClientsAsync(string query)
    {
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