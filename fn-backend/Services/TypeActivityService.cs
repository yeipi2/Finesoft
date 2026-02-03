using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class TypeActivityService : ITypeActivityService
{
     private readonly ApplicationDbContext _context;

    public TypeActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TypeActivity>> GetTypeActivitiesAsync()
    {
        return await _context.TypeActivities.ToListAsync();
    }

    public async Task<TypeActivity?> GetTypeActivityByIdAsync(int id)
    {
        return await _context.TypeActivities.FindAsync(id);
    }

    public async Task<ServiceResult<TypeActivity>> CreateTypeActivityAsync(TypeActivityDto dto)
    {
        var typeActivity = new TypeActivity
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.TypeActivities.Add(typeActivity);
        await _context.SaveChangesAsync();

        return ServiceResult<TypeActivity>.Success(typeActivity);
    }

    public async Task<ServiceResult<bool>> UpdateTypeActivityAsync(int id, TypeActivityDto dto)
    {
        var typeActivity = await _context.TypeActivities.FindAsync(id);
        if (typeActivity == null)
        {
            return ServiceResult<bool>.Failure("Tipo de actividad no encontrado");
        }

        typeActivity.Name = dto.Name;
        typeActivity.Description = dto.Description;
        typeActivity.IsActive = dto.IsActive;

        _context.TypeActivities.Update(typeActivity);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteTypeActivityAsync(int id)
    {
        var typeActivity = await _context.TypeActivities.FindAsync(id);
        if (typeActivity == null)
        {
            return ServiceResult<bool>.Failure("Tipo de actividad no encontrado");
        }

        // Verificar si hay servicios usando este tipo
        var hasServices = await _context.Services.AnyAsync(s => s.TypeActivityId == id);
        if (hasServices)
        {
            return ServiceResult<bool>.Failure("No se puede eliminar el tipo de actividad porque tiene servicios asociados");
        }

        _context.TypeActivities.Remove(typeActivity);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }
}