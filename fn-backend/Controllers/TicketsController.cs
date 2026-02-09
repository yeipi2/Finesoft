using fs_backend.DTO;
using fs_backend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace fs_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    // ✅ HELPER: Obtener ID del usuario actual
    private string? GetCurrentUserId()
        => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // ✅ HELPER: Verificar si el usuario tiene un rol específico
    private bool IsInRole(string role)
        => User.IsInRole(role);

    /// <summary>
    /// GET: api/tickets
    /// - Admin/Administracion: Ven TODOS los tickets
    /// - Empleado: Solo ve tickets asignados a él
    /// - Cliente: Solo ve tickets creados por él
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int? serviceId = null,
        [FromQuery] string? userId = null)
    {
        var currentUserId = GetCurrentUserId();

        // Si es Empleado, solo puede ver sus tickets asignados
        if (IsInRole("Empleado"))
        {
            userId = currentUserId; // Forzar filtro por su ID
        }

        // Si es Cliente, solo puede ver tickets que él creó
        if (IsInRole("Cliente"))
        {
            userId = currentUserId; // Forzar filtro por su ID
        }

        var tickets = await _ticketService.GetTicketsAsync(status, priority, serviceId, userId);
        return Ok(tickets);
    }

    /// <summary>
    /// GET: api/tickets/{id}
    /// - Empleado: Solo puede ver tickets asignados a él
    /// - Cliente: Solo puede ver tickets creados por él
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> GetTicketById(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
        {
            return NotFound(new { message = "Ticket no encontrado" });
        }

        var currentUserId = GetCurrentUserId();

        // Si es Empleado, validar que el ticket esté asignado a él
        if (IsInRole("Empleado") && ticket.AssignedToUserId != currentUserId)
        {
            return Forbid(); // 403 Forbidden
        }

        // Si es Cliente, validar que el ticket fue creado por él
        if (IsInRole("Cliente") && ticket.CreatedByUserId != currentUserId)
        {
            return Forbid();
        }

        return Ok(ticket);
    }

    /// <summary>
    /// POST: api/tickets
    /// - Admin/Administracion: Pueden crear tickets completos
    /// - Empleado: Pueden crear tickets completos
    /// - Cliente: Solo pueden poner Title y Description
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanCreateTicket")]
    public async Task<IActionResult> CreateTicket(TicketDto ticketDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        // ✅ Si es Cliente, LIMPIAR campos que no puede llenar
        if (IsInRole("Cliente"))
        {
            ticketDto.ProjectId = 0;
            ticketDto.ServiceId = 0;
            ticketDto.Status = "Abierto";
            ticketDto.Priority = "Media";
            ticketDto.AssignedToUserId = null;
            ticketDto.EstimatedHours = 0;
            ticketDto.ActualHours = 0;
        }

        var result = await _ticketService.CreateTicketAsync(ticketDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetTicketById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/tickets/{id}
    /// - Empleado: Solo puede editar tickets asignados a él
    /// - Cliente: NO puede editar (podrías permitirlo si lo necesitas)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> UpdateTicket(int id, TicketDto ticketDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        // ✅ Si es Empleado, validar que el ticket esté asignado a él
        if (IsInRole("Empleado"))
        {
            var ticket = await _ticketService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket no encontrado" });
            }

            if (ticket.AssignedToUserId != userId)
            {
                return Forbid(); // No puede editar tickets de otros
            }
        }

        var result = await _ticketService.UpdateTicketAsync(id, ticketDto, userId);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Ticket actualizado exitosamente" });
    }

    /// <summary>
    /// DELETE: api/tickets/{id}
    /// - Solo Admin y Administracion pueden eliminar
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOrAdministracion")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var result = await _ticketService.DeleteTicketAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Ticket eliminado exitosamente" });
    }

    // ========== COMENTARIOS ==========

    [HttpPost("{id}/comments")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> AddComment(int id, TicketCommentDto commentDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _ticketService.AddCommentAsync(id, commentDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    // ========== ESTADÍSTICAS ==========

    [HttpGet("stats")]
    [Authorize(Policy = "CanViewReports")]
    public async Task<IActionResult> GetTicketStats([FromQuery] string? userId = null)
    {
        var currentUserId = GetCurrentUserId();

        // Si es Empleado, solo puede ver sus propias estadísticas
        if (IsInRole("Empleado"))
        {
            userId = currentUserId;
        }

        var stats = await _ticketService.GetTicketStatsAsync(userId);
        return Ok(stats);
    }

    // ========== ACTIVIDADES ==========

    [HttpPost("{id}/activities")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> AddActivity(int id, TicketActivityDto activityDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _ticketService.AddActivityAsync(id, activityDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id}/activities/{activityId}")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> UpdateActivity(int id, int activityId, TicketActivityDto activityDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _ticketService.UpdateActivityAsync(id, activityId, activityDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id}/activities/{activityId}")]
    [Authorize(Policy = "AdminOrAdministracion")]
    public async Task<IActionResult> DeleteActivity(int id, int activityId)
    {
        var result = await _ticketService.DeleteActivityAsync(id, activityId);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(new { message = "Actividad eliminada exitosamente" });
    }

    [HttpPost("{id}/activities/{activityId}/complete")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> CompleteActivity(int id, int activityId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _ticketService.CompleteActivityAsync(id, activityId, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Actividad completada exitosamente" });
    }
}