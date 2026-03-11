using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<EmployeeService> _logger;
    private readonly ICacheService _cache;

    public EmployeeService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ILogger<EmployeeService> logger,
        ICacheService cache)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync()
    {
        return await _cache.GetOrSetAsync(
            "employees:all",
            async () =>
            {
                var employees = await _context.Employees
                    .OrderBy(e => e.FullName)
                    .ToListAsync();

                var employeeDtos = new List<EmployeeDto>();

                foreach (var emp in employees)
                {
                    var user = await _userManager.FindByIdAsync(emp.UserId);
                    var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

                    employeeDtos.Add(MapToDto(emp, user, roles));
                }

                return employeeDtos;
            },
            TimeSpan.FromMinutes(15)
        ) ?? new List<EmployeeDto>();
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var cacheKey = $"employees:id:{id}";

        return await _cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return null;

                var user = await _userManager.FindByIdAsync(employee.UserId);
                var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

                return MapToDto(employee, user, roles);
            },
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task<EmployeeDto?> GetEmployeeByUserIdAsync(string userId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null) return null;

        return await GetEmployeeByIdAsync(employee.Id);
    }

    /// <summary>
    /// Cambia el estado activo/inactivo del empleado.
    /// Si se desactiva, desasigna todos sus tickets abiertos (Abierto, En Progreso, En Revisión).
    /// </summary>
    public async Task<ServiceResult<ToggleEmployeeResult>> ToggleEmployeeStatusAsync(int id)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return ServiceResult<ToggleEmployeeResult>.Failure("Empleado no encontrado");

            employee.IsActive = !employee.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            int unassignedCount = 0;

            if (!employee.IsActive)
            {
                var statusesToUnassign = new[] { "Abierto", "En Progreso", "En Revisión" };

                var ticketsToUnassign = await _context.Tickets
                    .Where(t => t.AssignedToUserId == employee.UserId
                                && statusesToUnassign.Contains(t.Status))
                    .ToListAsync();

                foreach (var ticket in ticketsToUnassign)
                {
                    ticket.AssignedToUserId = null;
                    ticket.UpdatedAt = DateTime.UtcNow;

                    _context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        TicketId = ticket.Id,
                        UserId = employee.UserId,
                        Action = "AssignedToChanged",
                        OldValue = employee.FullName,
                        NewValue = "Sin asignar",
                        ChangedAt = DateTime.UtcNow
                    });
                }

                _context.Tickets.UpdateRange(ticketsToUnassign);
                unassignedCount = ticketsToUnassign.Count;

                _logger.LogInformation(
                    "🔄 Empleado {Id} desactivado. {Count} ticket(s) desasignados.",
                    id, unassignedCount);
            }

            await SyncUserAccessStateAsync(employee.UserId, employee.IsActive);

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            await _cache.InvalidateAsync("employees:all");
            await _cache.InvalidateAsync($"employees:id:{id}");

            return ServiceResult<ToggleEmployeeResult>.Success(new ToggleEmployeeResult
            {
                Success = true,
                UnassignedTickets = unassignedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al cambiar estado del empleado {Id}", id);
            return ServiceResult<ToggleEmployeeResult>.Failure($"Error al cambiar estado: {ex.Message}");
        }
    }

    public async Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(EmployeeDto dto)
    {
        var validRoles = new[] { "Empleado", "Supervisor", "Administracion" };
        if (!validRoles.Contains(dto.RoleName))
            return ServiceResult<EmployeeDto>.Failure("El rol debe ser Empleado, Supervisor o Administracion");

        if (string.IsNullOrEmpty(dto.Password))
            return ServiceResult<EmployeeDto>.Failure("La contraseña es requerida para crear un empleado");

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var userResult = await _userManager.CreateAsync(user, dto.Password);
        if (!userResult.Succeeded)
            return ServiceResult<EmployeeDto>.Failure(userResult.Errors.Select(e => e.Description));

        var roleResult = await _userManager.AddToRoleAsync(user, dto.RoleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ServiceResult<EmployeeDto>.Failure(roleResult.Errors.Select(e => e.Description));
        }

        var employee = new Employee
        {
            UserId = user.Id,
            FullName = dto.FullName,
            Phone = dto.Phone,
            Position = dto.Position,
            Department = dto.Department,
            HireDate = dto.HireDate,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            dto.Id = employee.Id;
            dto.UserId = user.Id;

            _logger.LogInformation("✅ Empleado creado: {FullName} (User: {UserId})", employee.FullName, user.Id);

            // Invalidar cache
            await _cache.InvalidateAsync("employees:all");

            return ServiceResult<EmployeeDto>.Success(dto);
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(user);
            _logger.LogError(ex, "❌ Error al crear empleado");
            return ServiceResult<EmployeeDto>.Failure($"Error al guardar el empleado: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UpdateEmployeeAsync(int id, EmployeeDto dto)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return ServiceResult<bool>.Failure("Empleado no encontrado");

            employee.FullName = dto.FullName;
            employee.Phone = dto.Phone;
            employee.Position = dto.Position;
            employee.Department = dto.Department;
            employee.HireDate = dto.HireDate;
            employee.IsActive = dto.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await SyncUserAccessStateAsync(employee.UserId, employee.IsActive);

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Empleado actualizado: {Id}", id);

            await _cache.InvalidateAsync("employees:all");
            await _cache.InvalidateAsync($"employees:id:{id}");

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al actualizar empleado {Id}", id);
            return ServiceResult<bool>.Failure($"Error al actualizar el empleado: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteEmployeeAsync(int id)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return ServiceResult<bool>.Failure("Empleado no encontrado");

            var user = await _userManager.FindByIdAsync(employee.UserId);
            if (user != null)
                await _userManager.DeleteAsync(user);

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Empleado eliminado: {Id}", id);

            await _cache.InvalidateAsync("employees:all");
            await _cache.InvalidateAsync($"employees:id:{id}");

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al eliminar empleado {Id}", id);
            return ServiceResult<bool>.Failure($"Error al eliminar el empleado: {ex.Message}");
        }
    }

    public async Task<List<EmployeeDto>> SearchEmployeesAsync(string query)
    {
        // Search no se cachea por ser dinámico
        var employees = await _context.Employees
            .Where(e => e.IsActive &&
                       (e.FullName.Contains(query) ||
                        e.Position.Contains(query) ||
                        e.Department.Contains(query)))
            .OrderBy(e => e.FullName)
            .Take(10)
            .ToListAsync();

        var result = new List<EmployeeDto>();

        foreach (var emp in employees)
        {
            var user = await _userManager.FindByIdAsync(emp.UserId);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            result.Add(MapToDto(emp, user, roles));
        }

        return result;
    }

    private static EmployeeDto MapToDto(Employee emp, IdentityUser? user, IList<string> roles) => new()
    {
        Id = emp.Id,
        UserId = emp.UserId,
        Email = user?.Email ?? string.Empty,
        RoleName = roles.FirstOrDefault() ?? string.Empty,
        FullName = emp.FullName,
        Phone = emp.Phone,
        Position = emp.Position,
        Department = emp.Department,
        HireDate = emp.HireDate,
        IsActive = emp.IsActive
    };

    private async Task SyncUserAccessStateAsync(string userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return;

        user.LockoutEnabled = !isActive;
        user.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;

        await _userManager.UpdateAsync(user);
    }
}
