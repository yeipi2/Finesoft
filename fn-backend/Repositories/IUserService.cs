using fn_backend.DTO;
using fs_backend.Util;

namespace fn_backend.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<ServiceResult<UserDto>> CreateUserAsync(UserDto userDto);
    Task<ServiceResult<bool>> UpdateUserAsync(string id, UserDto updateUserDto);
    Task<ServiceResult<bool>> DeleteUserAsync(string id);
    Task<ServiceResult<bool>> ChangePasswordAsync(string id, ChangePasswordDto changePasswordDto);
}