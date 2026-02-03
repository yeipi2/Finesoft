using fn_backend.DTO;
using fn_backend.Services;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            userDtos.Add(await MapUserToDto(user));
        }

        return userDtos;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user == null ? null : await MapUserToDto(user);
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(UserDto userDto)
    {
        if (string.IsNullOrEmpty(userDto.Password))
        {
            return ServiceResult<UserDto>.Failure("La contraseña es requerida para crear un usuario");
        }

        var user = new IdentityUser
        {
            UserName = userDto.UserName,
            Email = userDto.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, userDto.Password);
        if (!result.Succeeded)
        {
            return ServiceResult<UserDto>.Failure(result.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, userDto.RoleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user); // si no se puede asignar el rol, eliminar el usuario recién creado
            return ServiceResult<UserDto>.Failure(roleResult.Errors.Select(e => e.Description));
        }

        var responseDto = await MapUserToDto(user);
        return ServiceResult<UserDto>.Success(responseDto);
    }

    public async Task<ServiceResult<bool>> UpdateUserAsync(string id, UserDto userDto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return ServiceResult<bool>.Failure($"Usuario con ID {id} no encontrado");
        }

        user.UserName = userDto.UserName;
        user.Email = userDto.Email;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ServiceResult<bool>.Failure(result.Errors.Select(e => e.Description));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        // también se quita la relacion con los roles actuales
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var addResult = await _userManager.AddToRoleAsync(user, userDto.RoleName);

        return addResult.Succeeded
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(addResult.Errors.Select(e => e.Description));
    }

    public async Task<ServiceResult<bool>> DeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return ServiceResult<bool>.Failure($"Usuario con ID {id} no encontrado");
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(result.Errors.Select(e => e.Description));
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(string id, ChangePasswordDto changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return ServiceResult<bool>.Failure($"Usuario con ID {id} no encontrado");
        }

        var result =
            await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);
        return result.Succeeded
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(result.Errors.Select(e => e.Description));
    }

    /*
     * Helper method to map IdentityUser to UserDto
     */
    private async Task<UserDto> MapUserToDto(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            RoleName = roles.FirstOrDefault() ?? string.Empty,
            Password = null
        };
    }
}