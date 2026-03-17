using Asp.Versioning;
using fs_backend.DTO;
using fs_backend.Repositories;
using fs_backend.Attributes;
using fs_backend.DTO.Common;
using fs_backend.Util;
using fs_backend.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using fs_backend.Hubs;
using fs_backend.Models;
using fs_backend.Services;

namespace fs_backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketsController> _logger;
    private readonly IHubContext<NotificationsHub> _notificationsHub;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;

    public TicketsController(
        ITicketService ticketService,
        ILogger<TicketsController> logger,
        IHubContext<NotificationsHub> notificationsHub,
        UserManager<IdentityUser> userManager,
        INotificationService notificationService,
        ApplicationDbContext context)
    {
        _ticketService = ticketService;
        _logger = logger;
        _notificationsHub = notificationsHub;
        _userManager = userManager;
        _notificationService = notificationService;
        _context = context;
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
        [FromQuery] PaginationQueryDto query,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int? serviceId = null,
        [FromQuery] string? userId = null,
        [FromQuery] bool byCreator = false)
    {
        var currentUserId = GetCurrentUserId();

        _logger.LogInformation("📥 GET /tickets - Status: {Status}, Priority: {Priority}, Page: {Page}, PageSize: {PageSize}, Search: {Search}",
            status, priority, query.Page, query.PageSize, query.Search);

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

        // 🆕 Usar paginación a nivel de base de datos
        var (tickets, total) = await _ticketService.GetTicketsPaginatedAsync(
            status: status,
            priority: priority,
            serviceId: serviceId,
            userId: userId,
            byCreator: byCreator,
            search: query.Search,
            sortField: query.Sort,
            sortDescending: string.IsNullOrEmpty(query.Sort) || !query.Sort.StartsWith("-"),
            page: query.NormalizedPage,
            pageSize: query.NormalizedPageSize
        );

        _logger.LogInformation("✅ Usuario {UserId} ({Role}) obtuvo {Count} tickets (total: {Total})",
            currentUserId, User.IsInRole("Cliente") ? "Cliente" : "Staff", tickets.Count, total);

        var pagedResult = PaginatedResponseDto<TicketDetailDto>.Create(tickets, total, query.NormalizedPage, query.NormalizedPageSize);

        return Ok(pagedResult);
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
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Ticket no encontrado");
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        // Si es Cliente, LIMPIAR campos que no puede llenar PERO mantener ProjectId si lo envía
        if (IsInRole("Cliente"))
        {
            // Validar que el ProjectId pertenezca al cliente si se envía
            if (ticketDto.ProjectId.HasValue && ticketDto.ProjectId.Value > 0)
            {
                var projectExists = await _context.Projects
                    .AnyAsync(p => p.Id == ticketDto.ProjectId.Value && p.Client.UserId == userId);
                if (!projectExists)
                {
                    // El proyecto no existe o no pertenece al cliente, ignorarlo
                    ticketDto.ProjectId = null;
                    _logger.LogWarning("⚠️ Cliente {UserId} intentó usar proyecto no autorizado {ProjectId}", userId, ticketDto.ProjectId);
                }
                // Si el proyecto es válido, mantenerlo
            }
            else
            {
                ticketDto.ProjectId = null;
            }

            ticketDto.ServiceId = 0;
            ticketDto.Status = "Abierto";
            ticketDto.Priority = "Media";
            ticketDto.AssignedToUserId = null;

            _logger.LogInformation("ℹ️ Cliente {UserId} creó ticket con ProjectId={ProjectId}", userId, ticketDto.ProjectId);
        }

        var result = await _ticketService.CreateTicketAsync(ticketDto, userId);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        // 🆕 Empleado puede editar cualquier ticket (sin restricción)
        // Solo Cliente tendría restricción pero Cliente no tiene permiso tickets.edit

        var result = await _ticketService.UpdateTicketAsync(id, ticketDto, userId);
        if (!result.Succeeded)
        {
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", result.Errors.FirstOrDefault() ?? "Ticket no encontrado");
        }

        _logger.LogInformation("✅ Usuario {UserId} actualizó ticket #{TicketId}", userId, id);

        return NoContent();
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
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", result.Errors.FirstOrDefault() ?? "Ticket no encontrado");
        }

        _logger.LogInformation("✅ Usuario {UserId} eliminó ticket #{TicketId}", userId, id);

        return NoContent();
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        var result = await _ticketService.AddCommentAsync(id, commentDto, userId);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        var result = await _ticketService.AddActivityAsync(id, activityDto, userId);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
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

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
        {
            return NotFound(new { message = "Ticket no encontrado" });
        }

        var assignedUser = await _userManager.FindByIdAsync(dto.AssignedToUserId);
        
        _logger.LogInformation("✅ Usuario {UserId} asignó ticket #{TicketId} a {AssignedTo}",
            userId, id, dto.AssignedToUserId);

        // Notificar al empleado asignado
        if (assignedUser != null)
        {
            var notification = new NotificationDto
            {
                Type = "ticket_assigned",
                Title = "Nuevo Ticket Asignado",
                Message = $"Se te ha asignado el ticket #{id} - {ticket.Title}",
                Link = $"/tickets/{ticket.Id}",
                IconClass = "bi bi-ticket-detailed",
                IconColor = "#6B46C1"
            };
            await _notificationService.SaveNotificationAsync(dto.AssignedToUserId, notification);
            await NotificationsHub.SendToUser(_notificationsHub, dto.AssignedToUserId, notification);
        }

        // Notificar a Admin y Administracion
        var adminNotification = new NotificationDto
        {
            Type = "ticket_assigned",
            Title = "Ticket Asignado",
            Message = $"El ticket #{id} - {ticket.Title} ha sido asignado a {assignedUser?.UserName ?? dto.AssignedToUserId}",
            Link = $"/tickets/{ticket.Id}",
            IconClass = "bi bi-person-check",
            IconColor = "#6B46C1"
        };
        await NotificationsHub.SendToAdmins(_notificationsHub, adminNotification);
        await NotificationsHub.SendToAdministracion(_notificationsHub, adminNotification);

        // Guardar notificaciones para Admin y Administracion
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var adminUsers2 = await _userManager.GetUsersInRoleAsync("Administracion");
        var allAdminUsers = adminUsers.Concat(adminUsers2).Distinct();

        foreach (var user in allAdminUsers)
        {
            await _notificationService.SaveNotificationAsync(user.Id, adminNotification);
        }

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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        var result = await _ticketService.CompleteActivityAsync(ticketId, activityId, userId);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        var result = await _ticketService.UpdateActivityAsync(ticketId, activityId, activityDto, userId);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        var result = await _ticketService.DeleteActivityAsync(ticketId, activityId);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
        }

        _logger.LogInformation("✅ Usuario {UserId} eliminó actividad {ActivityId} del ticket #{TicketId}",
            userId, activityId, ticketId);

        return NoContent();
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
            return this.ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", "Usuario no autenticado");
        }

        var result = await _ticketService.UpdateTicketStatusAsync(id, request.Status, userId);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
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
