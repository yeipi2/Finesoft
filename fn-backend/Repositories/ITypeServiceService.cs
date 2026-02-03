using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface ITypeServiceService
{
    Task<IEnumerable<TypeService>> GetTypeServicesAsync();
    Task<TypeService?> GetTypeServiceByIdAsync(int id);
    Task<ServiceResult<TypeService>> CreateTypeServiceAsync(TypeServiceDto dto);
    Task<ServiceResult<bool>> UpdateTypeServiceAsync(int id, TypeServiceDto dto);
    Task<ServiceResult<bool>> DeleteTypeServiceAsync(int id);
}