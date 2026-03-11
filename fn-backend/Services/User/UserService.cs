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

        if (addResult.Succeeded && !string.IsNullOrWhiteSpace(userDto.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pwResult = await _userManager.ResetPasswordAsync(user, token, userDto.Password);

            if (!pwResult.Succeeded)
                return ServiceResult<bool>.Failure(pwResult.Errors.Select(e => e.Description));
        }

        return addResult.Succeeded
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(addResult.Errors.Select(e => e.Description));
    }

    public async Task<ServiceResult<bool>> DeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return ServiceResult<bool>.Failure($"Usuario con ID {id} no encontrado");

        var targetIsActive = !IsUserActive(user);

        SetUserAccessState(user, targetIsActive);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult<bool>.Failure(result.Errors.Select(e => e.Description));

        await SyncCatalogStatusAsync(user.Id, targetIsActive);

        return ServiceResult<bool>.Success(true);
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
        var roleName = roles.FirstOrDefault() ?? string.Empty;

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            RoleName = roleName,
            Password = null,
            IsActive = IsUserActive(user),
            DisplayName = await ResolveDisplayNameAsync(user, roleName)
        };
    }

    private static bool IsUserActive(IdentityUser user)
        => !(user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);

    private static void SetUserAccessState(IdentityUser user, bool isActive)
    {
        user.LockoutEnabled = !isActive;
        user.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
    }

    private async Task SyncCatalogStatusAsync(string userId, bool isActive)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee is not null)
        {
            employee.IsActive = isActive;
            employee.UpdatedAt = DateTime.UtcNow;
        }

        var client = await _context.Clients
            .Include(c => c.Projects)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (client is not null)
        {
            client.IsActive = isActive;
            client.UpdatedAt = DateTime.UtcNow;

            if (client.Projects is not null && client.Projects.Any())
            {
                foreach (var project in client.Projects)
                    project.IsActive = isActive;
            }
        }

        if (employee is not null || client is not null)
            await _context.SaveChangesAsync();
    }

    private async Task<string> ResolveDisplayNameAsync(IdentityUser user, string roleName)
    {
        if (EmployeeRoles.Contains(roleName))
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(employee?.FullName))
                return employee.FullName;
        }

        if (roleName.Equals("Cliente", StringComparison.OrdinalIgnoreCase))
        {
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == user.Id)
                ?? await _context.Clients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == user.Email);

            if (!string.IsNullOrWhiteSpace(client?.CompanyName))
                return client.CompanyName;
        }

        return user.UserName ?? user.Email ?? string.Empty;
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

    // UserService.cs — agregar este método a la clase
    public async Task<bool> IsEmailTakenAsync(string email, string? excludeUserId = null)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim().ToLower());

        if (user == null)
            return false; // Email libre

        // Si estamos editando, el email puede pertenecer al mismo usuario → no es conflicto
        if (!string.IsNullOrEmpty(excludeUserId) && user.Id == excludeUserId)
            return false;

        return true; // Email ya está tomado por otro usuario
    }
}
