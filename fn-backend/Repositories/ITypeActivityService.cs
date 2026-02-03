using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface ITypeActivityService
{
    Task<IEnumerable<TypeActivity>> GetTypeActivitiesAsync();
    Task<TypeActivity?> GetTypeActivityByIdAsync(int id);
    Task<ServiceResult<TypeActivity>> CreateTypeActivityAsync(TypeActivityDto dto);
    Task<ServiceResult<bool>> UpdateTypeActivityAsync(int id, TypeActivityDto dto);
    Task<ServiceResult<bool>> DeleteTypeActivityAsync(int id);
}