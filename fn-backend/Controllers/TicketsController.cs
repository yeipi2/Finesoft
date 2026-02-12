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
    /// - Admin/Administracion/Empleado: Ven TODOS los tickets
    /// - Cliente: Solo ve sus tickets creados
    /// </summary>
    [HttpGet]
    [RequirePermission("tickets.view")]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int? serviceId = null,
        [FromQuery] string? userId = null,
        [FromQuery] bool byCreator = false)
    {
        var currentUserId = GetCurrentUserId();

        // 🆕 Solo Cliente filtra por creador, los demás ven TODO
        if (IsInRole("Cliente"))
        {
            userId = currentUserId;
            byCreator = true; // Cliente busca por creador
        }
        // Admin, Administracion, Empleado, Supervisor ven TODOS
        else
        {
            userId = null; // Ver todos los tickets
            byCreator = false;
        }

        var tickets = await _ticketService.GetTicketsAsync(status, priority, serviceId, userId, byCreator);

        _logger.LogInformation("✅ Usuario {UserId} ({Role}) obtuvo {Count} tickets",
            currentUserId, User.IsInRole("Cliente") ? "Cliente" : "Staff", tickets.Count());

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

        // 🆕 Solo Cliente tiene restricción de acceso
        if (IsInRole("Cliente") && ticket.CreatedByUserId != currentUserId)
        {
            _logger.LogWarning("⛔ Cliente {UserId} intentó acceder al ticket {TicketId} de otro usuario",
                currentUserId, id);
            return Forbid();
        }

        // Admin, Administracion, Empleado, Supervisor pueden ver cualquier ticket
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
            ticketDto.ProjectId = null;
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

        // 🆕 Empleado puede editar cualquier ticket (sin restricción)
        // Solo Cliente tendría restricción pero Cliente no tiene permiso tickets.edit

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
    public async Task<IActionResult> GetTicketStats([FromQuery] string? userId = null, [FromQuery] bool byCreator = false)
    {
        var currentUserId = GetCurrentUserId();

        // 🆕 Solo Cliente filtra estadísticas
        if (IsInRole("Cliente"))
        {
            userId = currentUserId;
            byCreator = true;
        }
        else
        {
            // Admin, Administracion, Empleado, Supervisor ven estadísticas de TODO
            userId = null;
            byCreator = false;
        }

        var stats = await _ticketService.GetTicketStatsAsync(userId, byCreator);

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

        var result = await _ticketService.DeleteActivityAsync(ticketId, activityId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} eliminó actividad {ActivityId} del ticket #{TicketId}",
            userId, activityId, ticketId);

        return Ok(new { message = "Actividad eliminada exitosamente" });
    }

    // ============================================================
    // ACTUALIZACIÓN 3: TicketsController.cs
    // Agregar este endpoint al final de la clase TicketsController
    // (antes del último corchete de cierre)
    // ============================================================

    /// <summary>
    /// PUT: api/tickets/{id}/status
    /// ⭐ REQUIERE: permiso "tickets.edit"
    /// Actualiza solo el estado del ticket
    /// </summary>
    [HttpPut("{id}/status")]
    [RequirePermission("tickets.edit")]
    public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] UpdateTicketStatusRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado" });
        }

        var result = await _ticketService.UpdateTicketStatusAsync(id, request.Status, userId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} cambió estado del ticket #{TicketId} a {NewStatus}",
            userId, id, request.Status);

        return Ok(new { success = true, message = "Estado actualizado correctamente" });
    }

    // 🆕 DTO para la solicitud de actualización de estado
    public class UpdateTicketStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}

public class AssignTicketDto
{
    public string AssignedToUserId { get; set; } = string.Empty;
}