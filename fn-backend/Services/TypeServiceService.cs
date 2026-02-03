using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class TypeServiceService : ITypeServiceService
{
    private readonly ApplicationDbContext _context;

    public TypeServiceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TypeService>> GetTypeServicesAsync()
    {
        return await _context.TypeServices.ToListAsync();
    }

    public async Task<TypeService?> GetTypeServiceByIdAsync(int id)
    {
        return await _context.TypeServices.FindAsync(id);
    }

    public async Task<ServiceResult<TypeService>> CreateTypeServiceAsync(TypeServiceDto dto)
    {
        var typeService = new TypeService
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.TypeServices.Add(typeService);
        await _context.SaveChangesAsync();

        return ServiceResult<TypeService>.Success(typeService);
    }

    public async Task<ServiceResult<bool>> UpdateTypeServiceAsync(int id, TypeServiceDto dto)
    {
        var typeService = await _context.TypeServices.FindAsync(id);
        if (typeService == null)
        {
            return ServiceResult<bool>.Failure("Tipo de servicio no encontrado");
        }

        typeService.Name = dto.Name;
        typeService.Description = dto.Description;
        typeService.IsActive = dto.IsActive;

        _context.TypeServices.Update(typeService);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteTypeServiceAsync(int id)
    {
        var typeService = await _context.TypeServices.FindAsync(id);
        if (typeService == null)
        {
            return ServiceResult<bool>.Failure("Tipo de servicio no encontrado");
        }

        var hasServices = await _context.Services.AnyAsync(s => s.TypeServiceId == id);
        if (hasServices)
        {
            return ServiceResult<bool>.Failure(
                "No se puede eliminar el tipo de servicio porque tiene servicios asociados");
        }

        _context.TypeServices.Remove(typeService);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }
}