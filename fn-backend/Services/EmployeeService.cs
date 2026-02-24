using fn_backend.DTO;
using fn_backend.Models;
using fs_backend.Identity;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ILogger<EmployeeService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync()
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
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return null;

        var user = await _userManager.FindByIdAsync(employee.UserId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

        return MapToDto(employee, user, roles);
    }

    public async Task<EmployeeDto?> GetEmployeeByUserIdAsync(string userId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null) return null;

        return await GetEmployeeByIdAsync(employee.Id);
    }

    public async Task<(bool Success, string? ErrorMessage)> ToggleEmployeeStatusAsync(int id)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return (false, "Empleado no encontrado");

            employee.IsActive = !employee.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🔄 Estado de empleado cambiado: {Id}", id);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al cambiar estado del empleado {Id}", id);
            return (false, ex.Message);
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

        var user = await _userManager.FindByIdAsync(employee.UserId);
        if (user != null)
        {
            user.Email = dto.Email;
            user.UserName = dto.Email;
            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.FirstOrDefault() != dto.RoleName)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, dto.RoleName);
            }
        }

        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Empleado actualizado: {Id}", id);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteEmployeeAsync(int id)
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
        return ServiceResult<bool>.Success(true);
    }

    public async Task<List<EmployeeDto>> SearchEmployeesAsync(string query)
    {
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
}