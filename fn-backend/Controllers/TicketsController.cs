using fs_backend.DTO;
using fs_backend.Repositories;
using fs_backend.Attributes; 
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
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketService ticketService,
        ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    // ========== HELPERS ==========

    private string? GetCurrentUserId()
        => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool IsInRole(string role)
        => User.IsInRole(role);

    // ========== ENDPOINTS CON PERMISOS GRANULARES ==========

    /// <summary>
    /// GET: api/tickets
    /// ⭐ REQUIERE: permiso "tickets.view"
    /// - Admin: Tiene el permiso automáticamente (siempre puede)
    /// - Otros roles: Solo si tienen el permiso asignado
    /// </summary>
    [HttpGet]
    [RequirePermission("tickets.view")] // ⭐ VALIDACIÓN DE PERMISO
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
            userId = currentUserId;
        }

        // Si es Cliente, solo puede ver tickets que él creó
        if (IsInRole("Cliente"))
        {
            userId = currentUserId;
        }

        var tickets = await _ticketService.GetTicketsAsync(status, priority, serviceId, userId);

        _logger.LogInformation("✅ Usuario {UserId} obtuvo {Count} tickets", currentUserId, tickets.Count());

        return Ok(tickets);
    }

    /// <summary>
    /// GET: api/tickets/{id}
    /// ⭐ REQUIERE: permiso "tickets.view_detail"
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("tickets.view_detail")]
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
            _logger.LogWarning("⛔ Empleado {UserId} intentó acceder al ticket {TicketId} no asignado",
                currentUserId, id);
            return Forbid();
        }

        // Si es Cliente, validar que el ticket fue creado por él
        if (IsInRole("Cliente") && ticket.CreatedByUserId != currentUserId)
        {
            _logger.LogWarning("⛔ Cliente {UserId} intentó acceder al ticket {TicketId} de otro usuario",
                currentUserId, id);
            return Forbid();
        }

        return Ok(ticket);
    }

    /// <summary>
    /// POST: api/tickets
    /// ⭐ REQUIERE: permiso "tickets.create"
    /// </summary>
    [HttpPost]
    [RequirePermission("tickets.create")]
    public async Task<IActionResult> CreateTicket(TicketDto ticketDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        // Si es Cliente, LIMPIAR campos que no puede llenar
        if (IsInRole("Cliente"))
        {
            ticketDto.ProjectId = 0;
            ticketDto.ServiceId = 0;
            ticketDto.Status = "Abierto";
            ticketDto.Priority = "Media";
            ticketDto.AssignedToUserId = null;
            ticketDto.EstimatedHours = 0;
            ticketDto.ActualHours = 0;

            _logger.LogInformation("ℹ️ Cliente {UserId} creó ticket con campos limitados", userId);
        }

        var result = await _ticketService.CreateTicketAsync(ticketDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} creó ticket #{TicketId}",
            userId, result.Data!.Id);

        return CreatedAtAction(nameof(GetTicketById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/tickets/{id}
    /// ⭐ REQUIERE: permiso "tickets.edit"
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("tickets.edit")]
    public async Task<IActionResult> UpdateTicket(int id, TicketDto ticketDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        // Si es Empleado, validar que el ticket esté asignado a él
        if (IsInRole("Empleado"))
        {
            var ticket = await _ticketService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket no encontrado" });
            }

            if (ticket.AssignedToUserId != userId)
            {
                _logger.LogWarning("⛔ Empleado {UserId} intentó editar ticket {TicketId} no asignado",
                    userId, id);
                return Forbid();
            }
        }

        var result = await _ticketService.UpdateTicketAsync(id, ticketDto, userId);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} actualizó ticket #{TicketId}", userId, id);

        return Ok(new { message = "Ticket actualizado exitosamente" });
    }

    /// <summary>
    /// DELETE: api/tickets/{id}
    /// ⭐ REQUIERE: permiso "tickets.delete"
    /// Típicamente solo Admin y Administracion tendrán este permiso
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("tickets.delete")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var userId = GetCurrentUserId();

        var result = await _ticketService.DeleteTicketAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} eliminó ticket #{TicketId}", userId, id);

        return Ok(new { message = "Ticket eliminado exitosamente" });
    }

    /// <summary>
    /// POST: api/tickets/{id}/comments
    /// ⭐ REQUIERE: permiso "tickets.comment"
    /// </summary>
    [HttpPost("{id}/comments")]
    [RequirePermission("tickets.comment")]
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

        _logger.LogInformation("✅ Usuario {UserId} agregó comentario al ticket #{TicketId}",
            userId, id);

        return Ok(result.Data);
    }

    /// <summary>
    /// POST: api/tickets/{id}/activities
    /// ⭐ REQUIERE: permiso "tickets.activity"
    /// </summary>
    [HttpPost("{id}/activities")]
    [RequirePermission("tickets.activity")]
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

        _logger.LogInformation("✅ Usuario {UserId} agregó actividad al ticket #{TicketId}",
            userId, id);

        return Ok(result.Data);
    }

    /// <summary>
    /// GET: api/tickets/stats
    /// ⭐ REQUIERE: permiso "tickets.stats"
    /// </summary>
    [HttpGet("stats")]
    [RequirePermission("tickets.stats")]
    public async Task<IActionResult> GetTicketStats([FromQuery] string? userId = null)
    {
        var currentUserId = GetCurrentUserId();

        // Si es Empleado, solo puede ver sus propias estadísticas
        if (IsInRole("Empleado"))
        {
            userId = currentUserId;
        }

        var stats = await _ticketService.GetTicketStatsAsync(userId);

        _logger.LogInformation("✅ Usuario {UserId} obtuvo estadísticas de tickets", currentUserId);

        return Ok(stats);
    }

    /// <summary>
    /// POST: api/tickets/{id}/assign
    /// ⭐ REQUIERE: permiso "tickets.assign"
    /// Solo Admin y Administracion suelen tener este permiso
    /// </summary>
    [HttpPost("{id}/assign")]
    [RequirePermission("tickets.assign")]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketDto dto)
    {
        var userId = GetCurrentUserId();

        // Aquí iría la lógica de asignación (necesitarías implementarla en el servicio)
        // Por ahora es un ejemplo

        _logger.LogInformation("✅ Usuario {UserId} asignó ticket #{TicketId} a {AssignedTo}",
            userId, id, dto.AssignedToUserId);

        return Ok(new { message = "Ticket asignado exitosamente" });
    }

    /// <summary>
    /// POST: api/tickets/{ticketId}/activities/{activityId}/complete
    /// ⭐ REQUIERE: permiso "tickets.activity"
    /// </summary>
    [HttpPost("{ticketId}/activities/{activityId}/complete")]
    [RequirePermission("tickets.activity")]
    public async Task<IActionResult> CompleteActivity(int ticketId, int activityId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _ticketService.CompleteActivityAsync(ticketId, activityId, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} completó actividad {ActivityId} del ticket #{TicketId}",
            userId, activityId, ticketId);

        return Ok(result.Data);
    }

    /// <summary>
    /// PUT: api/tickets/{ticketId}/activities/{activityId}
    /// ⭐ REQUIERE: permiso "tickets.activity"
    /// </summary>
    [HttpPut("{ticketId}/activities/{activityId}")]
    [RequirePermission("tickets.activity")]
    public async Task<IActionResult> UpdateActivity(int ticketId, int activityId, [FromBody] TicketActivityDto activityDto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _ticketService.UpdateActivityAsync(ticketId, activityId, activityDto, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} actualizó actividad {ActivityId} del ticket #{TicketId}",
            userId, activityId, ticketId);

        return Ok(result.Data);
    }

    /// <summary>
    /// DELETE: api/tickets/{ticketId}/activities/{activityId}
    /// ⭐ REQUIERE: permiso "tickets.activity"
    /// </summary>
    [HttpDelete("{ticketId}/activities/{activityId}")]
    [RequirePermission("tickets.activity")]
    public async Task<IActionResult> DeleteActivity(int ticketId, int activityId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        // ⭐ CAMBIO: Solo pasar ticketId y activityId (sin userId)
        var result = await _ticketService.DeleteActivityAsync(ticketId, activityId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} eliminó actividad {ActivityId} del ticket #{TicketId}",
            userId, activityId, ticketId);

        return Ok(new { message = "Actividad eliminada exitosamente" });
    }
}



// DTO auxiliar para asignación
public class AssignTicketDto
{
    public string AssignedToUserId { get; set; } = string.Empty;
}