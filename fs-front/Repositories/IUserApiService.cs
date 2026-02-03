using fs_front.DTO;

namespace fs_front.Services;

public interface IUserApiService
{
    Task<List<UserDto>?> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<(bool Success, UserDto? CreatedUser, string? ErrorMessage)> CreateUserAsync(UserDto user);
    Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(string id, UserDto user);
    Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(string id);
}