using fn_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TypeActivitiesController : ControllerBase
{
    private readonly ITypeActivityService _typeActivityService;

    public TypeActivitiesController(ITypeActivityService typeActivityService)
    {
        _typeActivityService = typeActivityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTypeActivities()
    {
        var typeActivities = await _typeActivityService.GetTypeActivitiesAsync();
        return Ok(typeActivities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTypeActivityById(int id)
    {
        var typeActivity = await _typeActivityService.GetTypeActivityByIdAsync(id);
        if (typeActivity == null)
        {
            return NotFound(new { message = "Tipo de actividad no encontrado" });
        }

        return Ok(typeActivity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTypeActivity(TypeActivityDto dto)
    {
        var result = await _typeActivityService.CreateTypeActivityAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetTypeActivityById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTypeActivity(int id, TypeActivityDto dto)
    {
        var result = await _typeActivityService.UpdateTypeActivityAsync(id, dto);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Tipo de actividad actualizado exitosamente" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTypeActivity(int id)
    {
        var result = await _typeActivityService.DeleteTypeActivityAsync(id);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Tipo de actividad eliminado exitosamente" });
    }
}