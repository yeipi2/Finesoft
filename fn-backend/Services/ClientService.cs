using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Util;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;

    public ClientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<Client>> CreateClientAsync(ClientDto dto)
    {
        var client = new Client
        {
            CompanyName = dto.CompanyName,
            ContactName = dto.ContactName,
            Email = dto.Email,
            Phone = dto.Phone,
            RFC = dto.RFC,
            Address = dto.Address,
            ServiceMode = dto.ServiceMode,
            MonthlyRate = dto.MonthlyRate,
            IsActive = dto.IsActive,
        };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return ServiceResult<Client>.Success(client);
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

        client.CompanyName = dto.CompanyName;
        client.ContactName = dto.ContactName;
        client.Email = dto.Email;
        client.Phone = dto.Phone;
        client.RFC = dto.RFC;
        client.Address = dto.Address;
        client.ServiceMode = dto.ServiceMode;
        client.MonthlyRate = dto.MonthlyRate;
        client.IsActive = dto.IsActive;

        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClientAsync(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return false;
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        return true;
    }
    // Agregar este método a la clase ClientService

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