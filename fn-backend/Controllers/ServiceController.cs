using fn_backend.DTO;
using fs_backend.Repositories;
using fs_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]

public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetServices()
    {
        var services = await _serviceService.GetServicesAsync();
        return Ok(services);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetServicesByProject(int projectId)
    {
        var services = await _serviceService.GetServicesByProjectAsync(projectId);
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetServiceById(int id)
    {
        var service = await _serviceService.GetServiceByIdAsync(id);
        if (service == null)
        {
            return NotFound(new { message = "Servicio no encontrado" });
        }

        return Ok(service);
    }

    [HttpPost]
    public async Task<IActionResult> CreateService(ServiceDto serviceDto)
    {
        var result = await _serviceService.CreateServiceAsync(serviceDto);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetServiceById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateService(int id, ServiceDto serviceDto)
    {
        var result = await _serviceService.UpdateServiceAsync(id, serviceDto);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Servicio actualizado exitosamente" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var result = await _serviceService.DeleteServiceAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Servicio eliminado exitosamente" });
    }
}