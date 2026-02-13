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

            employeeDtos.Add(new EmployeeDto
            {
                Id = emp.Id,
                UserId = emp.UserId,
                Email = user?.Email ?? string.Empty,
                RoleName = roles.FirstOrDefault() ?? string.Empty,
                FullName = emp.FullName,
                RFC = emp.RFC,
                CURP = emp.CURP,
                Position = emp.Position,
                Department = emp.Department,
                Phone = emp.Phone,
                Address = emp.Address,
                HireDate = emp.HireDate,
                Salary = emp.Salary,
                IsActive = emp.IsActive,
                EmergencyContactName = emp.EmergencyContactName,
                EmergencyContactPhone = emp.EmergencyContactPhone,
                Notes = emp.Notes
            });
        }

        return employeeDtos;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return null;

        var user = await _userManager.FindByIdAsync(employee.UserId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

        return new EmployeeDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            Email = user?.Email ?? string.Empty,
            RoleName = roles.FirstOrDefault() ?? string.Empty,
            FullName = employee.FullName,
            RFC = employee.RFC,
            CURP = employee.CURP,
            Position = employee.Position,
            Department = employee.Department,
            Phone = employee.Phone,
            Address = employee.Address,
            HireDate = employee.HireDate,
            Salary = employee.Salary,
            IsActive = employee.IsActive,
            EmergencyContactName = employee.EmergencyContactName,
            EmergencyContactPhone = employee.EmergencyContactPhone,
            Notes = employee.Notes
        };
    }

    public async Task<EmployeeDto?> GetEmployeeByUserIdAsync(string userId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null) return null;

        return await GetEmployeeByIdAsync(employee.Id);
    }

    public async Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(EmployeeDto dto)
    {
        // ✅ 1. Validar que el rol sea válido para empleados
        var validRoles = new[] { "Empleado", "Supervisor", "Administracion" };
        if (!validRoles.Contains(dto.RoleName))
        {
            return ServiceResult<EmployeeDto>.Failure(
                "El rol debe ser Empleado, Supervisor o Administracion");
        }

        // ✅ 2. Crear el usuario primero
        if (string.IsNullOrEmpty(dto.Password))
        {
            return ServiceResult<EmployeeDto>.Failure(
                "La contraseña es requerida para crear un empleado");
        }

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var userResult = await _userManager.CreateAsync(user, dto.Password);
        if (!userResult.Succeeded)
        {
            return ServiceResult<EmployeeDto>.Failure(
                userResult.Errors.Select(e => e.Description));
        }

        // ✅ 3. Asignar rol
        var roleResult = await _userManager.AddToRoleAsync(user, dto.RoleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ServiceResult<EmployeeDto>.Failure(
                roleResult.Errors.Select(e => e.Description));
        }

        // ✅ 4. Crear el empleado
        var employee = new Employee
        {
            UserId = user.Id,
            FullName = dto.FullName,
            RFC = dto.RFC,
            CURP = dto.CURP,
            Position = dto.Position,
            Department = dto.Department,
            Phone = dto.Phone,
            Address = dto.Address,
            HireDate = dto.HireDate,
            Salary = dto.Salary,
            IsActive = dto.IsActive,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            dto.Id = employee.Id;
            dto.UserId = user.Id;

            _logger.LogInformation(
                "✅ Empleado creado: {FullName} (User: {UserId})",
                employee.FullName, user.Id);

            return ServiceResult<EmployeeDto>.Success(dto);
        }
        catch (Exception ex)
        {
            // Rollback: eliminar usuario si falla la creación del empleado
            await _userManager.DeleteAsync(user);
            _logger.LogError(ex, "❌ Error al crear empleado");
            return ServiceResult<EmployeeDto>.Failure(
                $"Error al guardar el empleado: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UpdateEmployeeAsync(int id, EmployeeDto dto)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return ServiceResult<bool>.Failure("Empleado no encontrado");
        }

        // ✅ Actualizar datos del empleado
        employee.FullName = dto.FullName;
        employee.RFC = dto.RFC;
        employee.CURP = dto.CURP;
        employee.Position = dto.Position;
        employee.Department = dto.Department;
        employee.Phone = dto.Phone;
        employee.Address = dto.Address;
        employee.HireDate = dto.HireDate;
        employee.Salary = dto.Salary;
        employee.IsActive = dto.IsActive;
        employee.EmergencyContactName = dto.EmergencyContactName;
        employee.EmergencyContactPhone = dto.EmergencyContactPhone;
        employee.Notes = dto.Notes;
        employee.UpdatedAt = DateTime.UtcNow;

        // ✅ Actualizar email y rol del usuario
        var user = await _userManager.FindByIdAsync(employee.UserId);
        if (user != null)
        {
            user.Email = dto.Email;
            user.UserName = dto.Email;
            await _userManager.UpdateAsync(user);

            // Cambiar rol si es necesario
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
        {
            return ServiceResult<bool>.Failure("Empleado no encontrado");
        }

        // ✅ Eliminar también el usuario asociado
        var user = await _userManager.FindByIdAsync(employee.UserId);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }

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
                        e.RFC.Contains(query) ||
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

            result.Add(new EmployeeDto
            {
                Id = emp.Id,
                UserId = emp.UserId,
                Email = user?.Email ?? string.Empty,
                RoleName = roles.FirstOrDefault() ?? string.Empty,
                FullName = emp.FullName,
                RFC = emp.RFC,
                Position = emp.Position,
                Department = emp.Department,
                Phone = emp.Phone,
                IsActive = emp.IsActive
            });
        }

        return result;
    }
}