using fn_backend.DTO;
using fs_backend.Attributes;
using fs_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fn_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/employees
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetEmployees()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obteniendo empleados", userId);

        var employees = await _employeeService.GetEmployeesAsync();
        return Ok(employees);
    }

    /// <summary>
    /// GET: api/employees/{id}
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("employees.view_detail")]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null)
            return NotFound(new { message = "Empleado no encontrado" });

        return Ok(employee);
    }

    /// <summary>
    /// GET: api/employees/user/{userId}
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetEmployeeByUserId(string userId)
    {
        var employee = await _employeeService.GetEmployeeByUserIdAsync(userId);
        if (employee == null)
            return NotFound(new { message = "Empleado no encontrado para este usuario" });

        return Ok(employee);
    }

    /// <summary>
    /// POST: api/employees
    /// </summary>
    [HttpPost]
    [RequirePermission("employees.create")]
    public async Task<IActionResult> CreateEmployee(EmployeeDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} creando empleado", userId);

        var result = await _employeeService.CreateEmployeeAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetEmployeeById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/employees/{id}
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("employees.edit")]
    public async Task<IActionResult> UpdateEmployee(int id, EmployeeDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} actualizando empleado {EmployeeId}", userId, id);

        var result = await _employeeService.UpdateEmployeeAsync(id, dto);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("no encontrado"))
                ? NotFound(result.Errors)
                : BadRequest(result.Errors);
        }

        return Ok(new { message = "Empleado actualizado exitosamente" });
    }

    /// <summary>
    /// DELETE: api/employees/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("employees.delete")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} eliminando empleado {EmployeeId}", userId, id);

        var result = await _employeeService.DeleteEmployeeAsync(id);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("no encontrado"))
                ? NotFound(result.Errors)
                : BadRequest(result.Errors);
        }

        return Ok(new { message = "Empleado eliminado exitosamente" });
    }

    /// <summary>
    /// PUT: api/employees/toggle-status/{id}
    /// Cambia estado activo/inactivo. Si se desactiva, desasigna tickets activos.
    /// Devuelve cuántos tickets fueron desasignados.
    /// </summary>
    [HttpPut("toggle-status/{id}")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var (success, errorMessage, unassignedTickets) = await _employeeService.ToggleEmployeeStatusAsync(id);

        if (!success)
            return BadRequest(errorMessage);

        // Devolvemos la cantidad de tickets desasignados para que el frontend pueda notificar
        return Ok(new { unassignedTickets });
    }

    /// <summary>
    /// GET: api/employees/search
    /// </summary>
    [HttpGet("search")]
    [Authorize]
    public async Task<IActionResult> SearchEmployees([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<EmployeeDto>());

        var employees = await _employeeService.SearchEmployeesAsync(query);
        return Ok(employees);
    }
}