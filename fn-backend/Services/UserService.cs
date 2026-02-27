using fn_backend.DTO;
using fn_backend.Services;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using fs_backend.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public UserService(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    // Roles que se asocian a la tabla Employees
    private static readonly HashSet<string> EmployeeRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Empleado", "Supervisor", "Administracion" };

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserDto>();
        foreach (var u in users)
            result.Add(await MapUserToDto(u));
        return result;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user == null ? null : await MapUserToDto(user);
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(UserDto userDto)
    {
        if (string.IsNullOrEmpty(userDto.Password))
            return ServiceResult<UserDto>.Failure("La contraseña es requerida para crear un usuario");

        var user = new IdentityUser
        {
            UserName = userDto.UserName,
            Email = userDto.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, userDto.Password);
        if (!result.Succeeded)
            return ServiceResult<UserDto>.Failure(result.Errors.Select(e => e.Description));

        var roleResult = await _userManager.AddToRoleAsync(user, userDto.RoleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ServiceResult<UserDto>.Failure(roleResult.Errors.Select(e => e.Description));
        }

        return ServiceResult<UserDto>.Success(await MapUserToDto(user));
    }

    public async Task<ServiceResult<bool>> UpdateUserAsync(string id, UserDto userDto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return ServiceResult<bool>.Failure($"Usuario con ID {id} no encontrado");

        user.UserName = userDto.UserName;
        user.Email = userDto.Email;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult<bool>.Failure(result.Errors.Select(e => e.Description));

        var currentRoles = await _userManager.GetRolesAsync(user);
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
            return ServiceResult<bool>.Failure($"Usuario con ID {id} no encontrado");

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(result.Errors.Select(e => e.Description));
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(string id, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return ServiceResult<bool>.Failure($"Usuario con ID {id} no encontrado");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        return result.Succeeded
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(result.Errors.Select(e => e.Description));
    }

    // ─────────────────────────────────────────────────────────
    //  GET MY PROFILE
    // ─────────────────────────────────────────────────────────
    public async Task<ProfileDto?> GetMyProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        var profile = new ProfileDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Role = role
        };

        // Empleado, Supervisor y Administracion → tabla Employees
        if (EmployeeRoles.Contains(role))
        {
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (emp is not null)
            {
                profile.EmployeeId = emp.Id;
                profile.FullName = emp.FullName;
                profile.Phone = emp.Phone;
                profile.Position = emp.Position;
                profile.Department = emp.Department;
                profile.HireDate = emp.HireDate;
            }
            // Si no tiene registro en Employees (p.ej. Admin puro sin empleado),
            // el perfil igual se devuelve con los datos de Identity.
        }
        else if (role == "Cliente")
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId)
                      ?? await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client is not null)
            {
                // Auto-reparar UserId si estaba desconectado
                if (client.UserId != userId)
                {
                    client.UserId = userId;
                    await _context.SaveChangesAsync();
                }

                profile.ClientId = client.Id;
                profile.FullName = client.ContactName;
                profile.CompanyName = client.CompanyName;
                profile.ContactName = client.ContactName;
                profile.Phone = client.Phone;
                profile.RFC = client.RFC;
                profile.Address = client.Address;
                profile.ServiceMode = client.ServiceMode;
                profile.MonthlyRate = client.MonthlyRate;
            }
        }
        // ⭐ Cargar imágenes desde UserProfiles
        var images = await GetUserImagesAsync(userId);
        profile.AvatarDataUrl = images.avatar;
        profile.CoverDataUrl = images.cover;

        return profile;

    
    }

    // ─────────────────────────────────────────────────────────
    //  UPDATE MY PROFILE  (solo Phone; Email opcional)
    // ─────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> UpdateMyProfileAsync(string userId, string role, ProfileUpdateDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ServiceResult<bool>.Failure("Usuario no encontrado");

        // Actualizar email en Identity solo si se envió
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            user.Email = dto.Email;
            user.UserName = dto.Email;
            await _userManager.UpdateAsync(user);
        }

        // Empleado, Supervisor y Administracion → tabla Employees
        if (EmployeeRoles.Contains(role))
        {
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (emp is not null)
            {
                if (dto.FullName != null) emp.FullName = dto.FullName;
                if (dto.Phone != null) emp.Phone = dto.Phone;
                if (dto.Position != null) emp.Position = dto.Position;
                if (dto.Department != null) emp.Department = dto.Department;
                emp.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        else if (role == "Cliente")
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId)
                      ?? await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client is not null)
            {
                if (client.UserId != userId) client.UserId = userId;
                if (dto.CompanyName != null) client.CompanyName = dto.CompanyName;
                if (dto.ContactName != null) client.ContactName = dto.ContactName;
                if (dto.Phone != null) client.Phone = dto.Phone;
                if (dto.RFC != null) client.RFC = dto.RFC;
                if (dto.Address != null) client.Address = dto.Address;
                if (dto.Email != null) client.Email = dto.Email;
                client.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        // Admin: solo se actualizó el email en Identity arriba

        return ServiceResult<bool>.Success(true);
    }

    // ─────────────────────────────────────────────────────────
    //  PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────
    //  SAVE / GET USER IMAGES
    // ─────────────────────────────────────────────────────────
    public async Task<ServiceResult<bool>> SaveUserImagesAsync(
        string userId, string? avatarDataUrl, string? coverDataUrl)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            profile = new fn_backend.Models.UserProfile { UserId = userId };
            _context.UserProfiles.Add(profile);
        }

        if (avatarDataUrl != null) profile.AvatarDataUrl = avatarDataUrl;
        if (coverDataUrl != null) profile.CoverDataUrl = coverDataUrl;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<(string? avatar, string? cover)> GetUserImagesAsync(string userId)
    {
        var profile = await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return (profile?.AvatarDataUrl, profile?.CoverDataUrl);
    }
}