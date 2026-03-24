using fs_backend.DTO;
using fs_backend.Hubs;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using fn_backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ICacheService _cache;
    private readonly IHubContext<NotificationsHub> _notificationsHub;
    private readonly INotificationService _notificationService;
    private readonly INotificationHelper _notificationHelper;

    public TicketService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ICacheService cache,
        IHubContext<NotificationsHub> notificationsHub,
        INotificationService notificationService,
        INotificationHelper notificationHelper)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
        _notificationsHub = notificationsHub;
        _notificationService = notificationService;
        _notificationHelper = notificationHelper;
    }

    // 🆕 MÉTODO ACTUALIZADO con parámetro byCreator
    public async Task<IEnumerable<TicketDetailDto>> GetTicketsAsync(
        string? status = null,
        string? priority = null,
        int? serviceId = null,
        string? userId = null,
        bool byCreator = false)
    {
        // Optimizado: incluir Project y Client en una sola consulta
        var query = _context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p != null ? p.Client : null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            query = query.Where(t => t.Priority == priority);
        }

        // 🆕 Filtrar por userId según byCreator
        if (!string.IsNullOrEmpty(userId))
        {
            if (byCreator)
            {
                query = query.Where(t => t.CreatedByUserId == userId);
            }
            else
            {
                query = query.Where(t => t.AssignedToUserId == userId);
            }
        }

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        // Obtener todos los userIds necesarios de una vez
        var allUserIds = tickets
            .Where(t => !string.IsNullOrEmpty(t.AssignedToUserId) || !string.IsNullOrEmpty(t.CreatedByUserId))
            .SelectMany(t => new[] { t.AssignedToUserId, t.CreatedByUserId }.Where(id => !string.IsNullOrEmpty(id)))
            .Distinct()
            .ToList();

        var users = new Dictionary<string, IdentityUser>();
        if (allUserIds.Any())
        {
            var userList = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .ToListAsync();
            users = userList.ToDictionary(u => u.Id);
        }

        // Obtener todos los empleados de una vez
        var allEmployeeUserIds = tickets
            .Where(t => !string.IsNullOrEmpty(t.AssignedToUserId))
            .Select(t => t.AssignedToUserId!)
            .Distinct()
            .ToList();
        
        var employees = new Dictionary<string, string>();
        if (allEmployeeUserIds.Any())
        {
            var employeeList = await _context.Employees
                .Where(e => allEmployeeUserIds.Contains(e.UserId))
                .Select(e => new { e.UserId, e.FullName })
                .ToListAsync();
            employees = employeeList.ToDictionary(e => e.UserId, e => e.FullName);
        }

        // Obtener todos los clientIds de una vez
        var allClientIds = tickets
            .Where(t => t.Project?.Client != null)
            .Select(t => t.Project!.Client!.Id)
            .Distinct()
            .ToList();
        
        // Agregar ClientId directo si existe
        var directClientIds = tickets.Where(t => t.ClientId.HasValue).Select(t => t.ClientId!.Value);
        allClientIds.AddRange(directClientIds);
        allClientIds = allClientIds.Distinct().ToList();

        var clients = new Dictionary<int, Client>();
        if (allClientIds.Any())
        {
            var clientList = await _context.Clients
                .Where(c => allClientIds.Contains(c.Id))
                .ToListAsync();
            clients = clientList.ToDictionary(c => c.Id);
        }

        var ticketDtos = new List<TicketDetailDto>();
        foreach (var ticket in tickets)
        {
            var assignedUser = !string.IsNullOrEmpty(ticket.AssignedToUserId) && users.ContainsKey(ticket.AssignedToUserId) 
                ? users[ticket.AssignedToUserId] : null;
            var createdByUser = !string.IsNullOrEmpty(ticket.CreatedByUserId) && users.ContainsKey(ticket.CreatedByUserId) 
                ? users[ticket.CreatedByUserId] : null;

            var assignedFullName = !string.IsNullOrEmpty(ticket.AssignedToUserId) && employees.ContainsKey(ticket.AssignedToUserId)
                ? employees[ticket.AssignedToUserId]
                : assignedUser?.UserName;

            // Obtener client info
            var clientId = ticket.Project?.Client?.Id ?? ticket.ClientId;
            var clientName = ticket.Project?.Client?.CompanyName;
            
            if (string.IsNullOrEmpty(clientName) && clientId.HasValue && clients.ContainsKey(clientId.Value))
            {
                clientName = clients[clientId.Value].CompanyName;
            }

            ticketDtos.Add(new TicketDetailDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                ServiceId = ticket.ServiceId ?? 0,
                ServiceName = string.Empty,
                ProjectId = ticket.ProjectId ?? 0,
                ProjectName = ticket.Project?.Name ?? "Sin asignar",
                ClientName = clientName ?? "Sin asignar",
                ClientId = clientId,
                Status = ticket.Status,
                Priority = ticket.Priority,
                AssignedToUserId = ticket.AssignedToUserId,
                AssignedToUserName = assignedFullName,
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByUserName = createdByUser?.UserName ?? string.Empty,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ClosedAt = ticket.ClosedAt,
                EstimatedHours = ticket.EstimatedHours,
                ActualHours = ticket.ActualHours,
                HourlyRate = 0,
                Comments = new List<TicketCommentDto>(),
                Activities = new List<TicketActivityDto>(),
                Attachments = new List<TicketAttachmentDto>()
            });
        }

        return ticketDtos;
    }

    public async Task<TicketDetailDto?> GetTicketByIdAsync(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p.Client)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Include(t => t.History)
            .Include(t => t.Activities)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return null;
        }

        return await MapToDetailDto(ticket);
    }

    public async Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(TicketDto ticketDto, string createdByUserId)
    {
        // Solo validar proyecto si está presente
        if (ticketDto.ProjectId.HasValue && ticketDto.ProjectId.Value > 0)
        {
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == ticketDto.ProjectId.Value);
            if (!projectExists)
            {
                return ServiceResult<TicketDetailDto>.Failure("El proyecto especificado no existe");
            }
        }

        // Validar usuario asignado (si existe)
        if (!string.IsNullOrEmpty(ticketDto.AssignedToUserId))
        {
            var userExists = await _userManager.FindByIdAsync(ticketDto.AssignedToUserId);
            if (userExists == null)
            {
                return ServiceResult<TicketDetailDto>.Failure("El usuario asignado no existe");
            }
        }

        var ticket = new Ticket
        {
            Title = ticketDto.Title,
            Description = ticketDto.Description,
            ProjectId = ticketDto.ProjectId.HasValue && ticketDto.ProjectId.Value > 0
                ? ticketDto.ProjectId.Value
                : null,
            ServiceId = ticketDto.ServiceId > 0 ? ticketDto.ServiceId : null,
            Status = ticketDto.Status,
            Priority = ticketDto.Priority,
            AssignedToUserId = ticketDto.AssignedToUserId,
            CreatedByUserId = createdByUserId,
            EstimatedHours = ticketDto.EstimatedHours ?? 0,
            ActualHours = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);

        var history = new TicketHistory
        {
            Ticket = ticket,
            UserId = createdByUserId,
            Action = "Created",
            NewValue = ticket.ProjectId.HasValue
                ? "Ticket creado"
                : "Ticket creado (pendiente de clasificar)",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        // Notificaciones por ticket creado usando el nuevo sistema (excluir al creador)
        var adminNotification = await _notificationHelper.CreateNotificationWithCreatorAsync(
            NotificationType.TicketCreatedByEmployee,
            "Nuevo Ticket Creado",
            $"Se ha creado el ticket #{ticket.Id} - {ticket.Title}",
            createdByUserId,
            $"/tickets/{ticket.Id}");

        // Notificar a Admin y Administracion (excluyendo al creador)
        await _notificationHelper.SendToAdminsAsync(adminNotification, excludeUserId: createdByUserId);

        // Notificar al empleado asignado si existe
        if (!string.IsNullOrEmpty(ticket.AssignedToUserId))
        {
            var assignedNotification = await _notificationHelper.CreateNotificationWithCreatorAsync(
                NotificationType.TicketAssigned,
                "Nuevo Ticket Asignado",
                $"Se te ha asignado el ticket #{ticket.Id} - {ticket.Title}",
                createdByUserId,
                $"/tickets/{ticket.Id}");
            await _notificationHelper.SendToUserAsync(ticket.AssignedToUserId, assignedNotification);
        }

        // Cargar Project solo si existe
        if (ticket.ProjectId.HasValue)
        {
            await _context.Entry(ticket)
                .Reference(t => t.Project)
                .Query()
                .Include(p => p.Client)
                .LoadAsync();
        }

        return ServiceResult<TicketDetailDto>.Success(await MapToDetailDto(ticket));
    }

    public async Task<ServiceResult<bool>> UpdateTicketAsync(int id, TicketDto ticketDto, string userId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return ServiceResult<bool>.Failure("Ticket no encontrado");
        }

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == ticketDto.ProjectId);
        if (!projectExists)
        {
            return ServiceResult<bool>.Failure("El proyecto especificado no existe");
        }

        if (!string.IsNullOrEmpty(ticketDto.AssignedToUserId))
        {
            var userExists = await _userManager.FindByIdAsync(ticketDto.AssignedToUserId);
            if (userExists == null)
            {
                return ServiceResult<bool>.Failure("El usuario asignado no existe");
            }
        }

        var changes = new List<TicketHistory>();

        if (ticket.Title != ticketDto.Title)
        {
            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "TitleChanged",
                OldValue = ticket.Title,
                NewValue = ticketDto.Title,
                ChangedAt = DateTime.UtcNow
            });
        }

        if (ticket.Status != ticketDto.Status)
        {
            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "StatusChanged",
                OldValue = ticket.Status,
                NewValue = ticketDto.Status,
                ChangedAt = DateTime.UtcNow
            });

            if (ticketDto.Status == "Cerrado")
            {
                ticket.ClosedAt = DateTime.UtcNow;
            }
        }

        if (ticket.Priority != ticketDto.Priority)
        {
            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "PriorityChanged",
                OldValue = ticket.Priority,
                NewValue = ticketDto.Priority,
                ChangedAt = DateTime.UtcNow
            });
        }

        if (ticket.AssignedToUserId != ticketDto.AssignedToUserId)
        {
            var oldUserName = ticket.AssignedToUserId != null
                ? (await _userManager.FindByIdAsync(ticket.AssignedToUserId))?.UserName
                : "Sin asignar";
            var newUserName = ticketDto.AssignedToUserId != null
                ? (await _userManager.FindByIdAsync(ticketDto.AssignedToUserId))?.UserName
                : "Sin asignar";

            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "AssignedToChanged",
                OldValue = oldUserName,
                NewValue = newUserName,
                ChangedAt = DateTime.UtcNow
            });

            // Notificar al empleado cuando se le asigna un ticket
            if (!string.IsNullOrEmpty(ticketDto.AssignedToUserId))
            {
                var assignedNotification = await _notificationHelper.CreateNotificationWithCreatorAsync(
                    NotificationType.TicketAssigned,
                    "Nuevo Ticket Asignado",
                    $"Se te ha asignado el ticket #{id} - {ticket.Title}",
                    userId,
                    $"/tickets/{id}");

                // Notificar al empleado asignado
                await _notificationHelper.SendToUserAsync(ticketDto.AssignedToUserId, assignedNotification);

                // Notificar a Admin y Administracion (excluir al usuario que asignó)
                await _notificationHelper.SendToAdminsAsync(assignedNotification, excludeUserId: userId);
            }
        }

        ticket.Title = ticketDto.Title;
        ticket.Description = ticketDto.Description;
        ticket.ProjectId = ticketDto.ProjectId.HasValue && ticketDto.ProjectId.Value > 0 
            ? ticketDto.ProjectId.Value 
            : null;
        ticket.ClientId = ticketDto.ClientId;
        ticket.ServiceId = ticketDto.ServiceId > 0 ? ticketDto.ServiceId : null;
        ticket.Status = ticketDto.Status;
        ticket.Priority = ticketDto.Priority;
        ticket.AssignedToUserId = ticketDto.AssignedToUserId;
        ticket.EstimatedHours = ticketDto.EstimatedHours ?? 0;
        ticket.ActualHours = ticketDto.ActualHours ?? 0;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.Tickets.Update(ticket);

        if (changes.Any())
        {
            _context.Set<TicketHistory>().AddRange(changes);
        }

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteTicketAsync(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);

        if (ticket == null)
        {
            return ServiceResult<bool>.Failure("Ticket no encontrado");
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<TicketCommentDto>> AddCommentAsync(int ticketId, TicketCommentDto commentDto,
        string userId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);

        if (ticket == null)
        {
            return ServiceResult<TicketCommentDto>.Failure("Ticket no encontrado");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<TicketCommentDto>.Failure("Usuario no encontrado");
        }

        var comment = new TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            Comment = commentDto.Comment,
            IsInternal = commentDto.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<TicketComment>().Add(comment);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "CommentAdded",
            NewValue = commentDto.IsInternal ? "Comentario interno agregado" : "Comentario agregado",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        // Notificar sobre nuevo comentario
        var commentNotification = await _notificationHelper.CreateNotificationWithCreatorAsync(
            NotificationType.TicketComment,
            "Nuevo Comentario",
            $"Nuevo comentario en ticket #{ticketId}",
            userId,
            $"/tickets/{ticketId}");

        // Notificar al creador del ticket si es diferente al que comenta
        if (!string.IsNullOrEmpty(ticket.CreatedByUserId) && ticket.CreatedByUserId != userId)
        {
            await _notificationHelper.SendToUserAsync(ticket.CreatedByUserId, commentNotification);
        }

        // Notificar al empleado asignado si es diferente al que comenta
        if (!string.IsNullOrEmpty(ticket.AssignedToUserId) && ticket.AssignedToUserId != userId)
        {
            await _notificationHelper.SendToUserAsync(ticket.AssignedToUserId, commentNotification);
        }

        // Notificar a Admin y Administracion (excluir al usuario que hizo el comentario)
        await _notificationHelper.SendToAdminsAsync(commentNotification, excludeUserId: userId);

        return ServiceResult<TicketCommentDto>.Success(new TicketCommentDto
        {
            Id = comment.Id,
            Comment = comment.Comment,
            IsInternal = comment.IsInternal,
            UserId = userId,
            UserName = user.UserName,
            CreatedAt = comment.CreatedAt
        });
    }

    // 🆕 NUEVO MÉTODO con paginación a nivel de base de datos
    public async Task<(List<TicketDetailDto> Items, int Total)> GetTicketsPaginatedAsync(
        string? status = null,
        string? priority = null,
        int? serviceId = null,
        string? userId = null,
        bool byCreator = false,
        string? search = null,
        string? sortField = null,
        bool sortDescending = true,
        int page = 1,
        int pageSize = 20)
    {
        // Construir query base con incluye necesarios
        var query = _context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p != null ? p.Client : null)
            .AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            if (byCreator)
            {
                query = query.Where(t => t.CreatedByUserId == userId);
            }
            else
            {
                query = query.Where(t => t.AssignedToUserId == userId);
            }
        }

        // Aplicar búsqueda en base de datos (busca en múltiples campos)
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(searchLower) ||
                (t.Description != null && t.Description.ToLower().Contains(searchLower)) ||
                (t.Project != null && t.Project.Name.ToLower().Contains(searchLower)) ||
                (t.Project != null && t.Project.Client != null && t.Project.Client.CompanyName.ToLower().Contains(searchLower)));
        }

        // Obtener total ANTES de paginar
        var total = await query.CountAsync();

        // Aplicar ordenamiento
        query = sortField?.ToLower() switch
        {
            "id" => sortDescending ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id),
            "title" => sortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "priority" => sortDescending
                ? query.OrderByDescending(t => t.Priority == "Urgente" ? 4 : t.Priority == "Alta" ? 3 : t.Priority == "Media" ? 2 : 1)
                : query.OrderBy(t => t.Priority == "Urgente" ? 4 : t.Priority == "Alta" ? 3 : t.Priority == "Media" ? 2 : 1),
            "status" => sortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "createdat" => sortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.Id) // default
        };

        // Aplicar paginación
        var skip = (page - 1) * pageSize;
        var tickets = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        // Obtener todos los userIds necesarios de una vez
        var allUserIds = tickets
            .Where(t => !string.IsNullOrEmpty(t.AssignedToUserId) || !string.IsNullOrEmpty(t.CreatedByUserId))
            .SelectMany(t => new[] { t.AssignedToUserId, t.CreatedByUserId }.Where(id => !string.IsNullOrEmpty(id)))
            .Distinct()
            .ToList();

        var users = new Dictionary<string, IdentityUser>();
        if (allUserIds.Any())
        {
            var userList = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .ToListAsync();
            users = userList.ToDictionary(u => u.Id);
        }

        // Obtener todos los empleados de una vez
        var allEmployeeUserIds = tickets
            .Where(t => !string.IsNullOrEmpty(t.AssignedToUserId))
            .Select(t => t.AssignedToUserId!)
            .Distinct()
            .ToList();

        var employees = new Dictionary<string, string>();
        if (allEmployeeUserIds.Any())
        {
            var employeeList = await _context.Employees
                .Where(e => allEmployeeUserIds.Contains(e.UserId))
                .Select(e => new { e.UserId, e.FullName })
                .ToListAsync();
            employees = employeeList.ToDictionary(e => e.UserId, e => e.FullName);
        }

        // Obtener todos los clientIds de una vez
        var allClientIds = tickets
            .Where(t => t.Project?.Client != null)
            .Select(t => t.Project!.Client!.Id)
            .Distinct()
            .ToList();

        var directClientIds = tickets.Where(t => t.ClientId.HasValue).Select(t => t.ClientId!.Value);
        allClientIds.AddRange(directClientIds);
        allClientIds = allClientIds.Distinct().ToList();

        var clients = new Dictionary<int, Client>();
        if (allClientIds.Any())
        {
            var clientList = await _context.Clients
                .Where(c => allClientIds.Contains(c.Id))
                .ToListAsync();
            clients = clientList.ToDictionary(c => c.Id);
        }

        // Mapear a DTOs
        var ticketDtos = new List<TicketDetailDto>();
        foreach (var ticket in tickets)
        {
            var assignedUser = !string.IsNullOrEmpty(ticket.AssignedToUserId) && users.ContainsKey(ticket.AssignedToUserId)
                ? users[ticket.AssignedToUserId] : null;
            var createdByUser = !string.IsNullOrEmpty(ticket.CreatedByUserId) && users.ContainsKey(ticket.CreatedByUserId)
                ? users[ticket.CreatedByUserId] : null;

            var assignedFullName = !string.IsNullOrEmpty(ticket.AssignedToUserId) && employees.ContainsKey(ticket.AssignedToUserId)
                ? employees[ticket.AssignedToUserId]
                : assignedUser?.UserName;

            var clientId = ticket.Project?.Client?.Id ?? ticket.ClientId;
            var clientName = ticket.Project?.Client?.CompanyName;

            if (string.IsNullOrEmpty(clientName) && clientId.HasValue && clients.ContainsKey(clientId.Value))
            {
                clientName = clients[clientId.Value].CompanyName;
            }

            ticketDtos.Add(new TicketDetailDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                ServiceId = ticket.ServiceId ?? 0,
                ServiceName = string.Empty,
                ProjectId = ticket.ProjectId ?? 0,
                ProjectName = ticket.Project?.Name ?? "Sin asignar",
                ClientName = clientName ?? "Sin asignar",
                ClientId = clientId,
                Status = ticket.Status,
                Priority = ticket.Priority,
                AssignedToUserId = ticket.AssignedToUserId,
                AssignedToUserName = assignedFullName,
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByUserName = createdByUser?.UserName ?? string.Empty,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ClosedAt = ticket.ClosedAt,
                EstimatedHours = ticket.EstimatedHours,
                ActualHours = ticket.ActualHours,
                HourlyRate = 0,
                Comments = new List<TicketCommentDto>(),
                Activities = new List<TicketActivityDto>(),
                Attachments = new List<TicketAttachmentDto>()
            });
        }

        return (ticketDtos, total);
    }

    // 🆕 MÉTODO OPTIMIZADO con contador en base de datos
    public async Task<TicketStatsDto> GetTicketStatsAsync(string? userId = null, bool byCreator = false)
    {
        // Crear clave de cache basada en parámetros
        var cacheKey = string.Format(CacheKeys.TicketStats, userId ?? "all", byCreator.ToString());

        return await _cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var baseQuery = _context.Tickets.AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    if (byCreator)
                    {
                        // Filtrar por creador (Cliente)
                        baseQuery = baseQuery.Where(t => t.CreatedByUserId == userId);
                    }
                    else
                    {
                        // Filtrar por asignado (Empleado)
                        baseQuery = baseQuery.Where(t => t.AssignedToUserId == userId);
                    }
                }

                // Contar directamente en la base de datos (mucho más eficiente)
                var open = await baseQuery.CountAsync(t => t.Status == "Abierto");
                var inProgress = await baseQuery.CountAsync(t => t.Status == "En Progreso");
                var inReview = await baseQuery.CountAsync(t => t.Status == "En Revisión");
                var closed = await baseQuery.CountAsync(t => t.Status == "Cerrado");
                var total = await baseQuery.CountAsync();

                // Obtener horas directamente de la base de datos
                var stats = await baseQuery
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        TotalEstimated = g.Sum(t => t.EstimatedHours),
                        TotalActual = g.Sum(t => t.ActualHours)
                    })
                    .FirstOrDefaultAsync();

                return new TicketStatsDto
                {
                    Open = open,
                    InProgress = inProgress,
                    InReview = inReview,
                    Closed = closed,
                    Total = total,
                    TotalEstimatedHours = stats?.TotalEstimated ?? 0,
                    TotalActualHours = stats?.TotalActual ?? 0
                };
            },
            TimeSpan.FromMinutes(5) // Cache corto de 5 minutos para stats
        ) ?? new TicketStatsDto();
    }

    // ========== MÉTODOS PARA ACTIVIDADES ==========

    public async Task<ServiceResult<TicketActivityDto>> AddActivityAsync(int ticketId, TicketActivityDto activityDto, string userId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Ticket no encontrado");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Usuario no encontrado");
        }

        var activity = new TicketActivity
        {
            TicketId = ticketId,
            Description = activityDto.Description,
            HoursSpent = activityDto.HoursSpent,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.Set<TicketActivity>().Add(activity);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "ActivityAdded",
            NewValue = $"Actividad agregada: {activityDto.Description}",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        return ServiceResult<TicketActivityDto>.Success(new TicketActivityDto
        {
            Id = activity.Id,
            TicketId = activity.TicketId,
            Description = activity.Description,
            HoursSpent = activity.HoursSpent,
            IsCompleted = activity.IsCompleted,
            CreatedAt = activity.CreatedAt,
            CompletedAt = activity.CompletedAt,
            CreatedByUserId = userId,
            CreatedByUserName = user.UserName ?? string.Empty
        });
    }

    public async Task<ServiceResult<TicketActivityDto>> UpdateActivityAsync(int ticketId, int activityId, TicketActivityDto activityDto, string userId)
    {
        var activity = await _context.Set<TicketActivity>()
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TicketId == ticketId);

        if (activity == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Actividad no encontrada");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Usuario no encontrado");
        }

        activity.Description = activityDto.Description;
        activity.HoursSpent = activityDto.HoursSpent;

        // Detectar si la actividad se está marcando como completada
        bool wasCompleted = activity.IsCompleted;
        bool nowCompleted = activityDto.IsCompleted;
        bool justCompleted = !wasCompleted && nowCompleted;

        if (nowCompleted)
        {
            activity.IsCompleted = true;
            activity.CompletedAt = DateTime.UtcNow;
        }

        _context.Set<TicketActivity>().Update(activity);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = justCompleted ? "ActivityCompleted" : "ActivityUpdated",
            NewValue = justCompleted
                ? $"Actividad completada: {activityDto.Description}"
                : $"Actividad actualizada: {activityDto.Description}",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        // Notificar cuando se completa una actividad
        if (justCompleted)
        {
            var activityNotification = await _notificationHelper.CreateNotificationWithCreatorAsync(
                NotificationType.TicketActivityCompleted,
                "Actividad Completada",
                $"Se completó la actividad '{activityDto.Description}' en ticket #{ticketId}",
                userId,
                $"/tickets/{ticketId}");

            // Notificar al creador del ticket
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null && !string.IsNullOrEmpty(ticket.CreatedByUserId) && ticket.CreatedByUserId != userId)
            {
                await _notificationHelper.SendToUserAsync(ticket.CreatedByUserId, activityNotification);
            }

            // Notificar a Admin y Administracion (excluir al usuario que agregó la actividad)
            await _notificationHelper.SendToAdminsAsync(activityNotification, excludeUserId: userId);
        }

        return ServiceResult<TicketActivityDto>.Success(new TicketActivityDto
        {
            Id = activity.Id,
            TicketId = activity.TicketId,
            Description = activity.Description,
            HoursSpent = activity.HoursSpent,
            IsCompleted = activity.IsCompleted,
            CreatedAt = activity.CreatedAt,
            CompletedAt = activity.CompletedAt,
            CreatedByUserId = activity.CreatedByUserId,
            CreatedByUserName = user.UserName ?? string.Empty
        });
    }

    public async Task<ServiceResult<bool>> DeleteActivityAsync(int ticketId, int activityId)
    {
        var activity = await _context.Set<TicketActivity>()
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TicketId == ticketId);

        if (activity == null)
        {
            return ServiceResult<bool>.Failure("Actividad no encontrada");
        }

        if (activity.IsCompleted)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.ActualHours -= activity.HoursSpent;
                _context.Tickets.Update(ticket);
            }
        }

        _context.Set<TicketActivity>().Remove(activity);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> CompleteActivityAsync(int ticketId, int activityId, string userId)
    {
        var activity = await _context.Set<TicketActivity>()
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TicketId == ticketId);

        if (activity == null)
        {
            return ServiceResult<bool>.Failure("Actividad no encontrada");
        }

        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            return ServiceResult<bool>.Failure("Ticket no encontrado");
        }

        activity.IsCompleted = true;
        activity.CompletedAt = DateTime.UtcNow;

        ticket.ActualHours += activity.HoursSpent;

        _context.Set<TicketActivity>().Update(activity);
        _context.Tickets.Update(ticket);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "ActivityCompleted",
            NewValue = $"Actividad completada: {activity.Description} ({activity.HoursSpent}h)",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> UpdateTicketStatusAsync(int ticketId, string newStatus, string userId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
        {
            return ServiceResult<bool>.Failure("Ticket no encontrado");
        }

        var oldStatus = ticket.Status;
        ticket.Status = newStatus;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (newStatus == "Cerrado")
        {
            ticket.ClosedAt = DateTime.UtcNow;
        }

        _context.Tickets.Update(ticket);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "StatusChanged",
            OldValue = oldStatus,
            NewValue = newStatus,
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);
        await _context.SaveChangesAsync();

        if (newStatus == "Cerrado" && ticket.Project?.Client != null)
        {
            // Obtener IDs de Admin y Administracion para evitar duplicados
            var adminUserIds = await _notificationHelper.GetUserIdsByRolesAsync(new[] { "Admin", "Administracion" });
            var adminUserIdSet = new HashSet<string>(adminUserIds);

            // Solo enviar "Tu ticket ha sido cerrado" si el creador NO es Admin ni Administración
            if (!string.IsNullOrEmpty(ticket.CreatedByUserId) && !adminUserIdSet.Contains(ticket.CreatedByUserId))
            {
                var clientNotification = await _notificationHelper.CreateNotificationWithCreatorAsync(
                    NotificationType.TicketClosed,
                    "Ticket Cerrado",
                    $"Tu ticket #{ticket.Id} - {ticket.Title} ha sido cerrado.",
                    userId,
                    $"/tickets/{ticket.Id}");
                await _notificationHelper.SendToUserAsync(ticket.CreatedByUserId, clientNotification);
            }

            // Notificar a Admin y Administracion (excluir al usuario que cerró el ticket)
            var adminNotification = await _notificationHelper.CreateNotificationWithCreatorAsync(
                NotificationType.TicketClosed,
                "Ticket Cerrado",
                $"El ticket #{ticket.Id} - {ticket.Title} de {ticket.Project.Client.CompanyName} ha sido cerrado.",
                userId,
                $"/tickets/{ticket.Id}");
            await _notificationHelper.SendToAdminsAsync(adminNotification, excludeUserId: userId);
        }

        return ServiceResult<bool>.Success(true);
    }

    private async Task<TicketDetailDto> MapToDetailDto(Ticket ticket)
    {
        IdentityUser? assignedUser = null;
        IdentityUser? createdByUser = null;

        if (!string.IsNullOrEmpty(ticket.AssignedToUserId))
        {
            assignedUser = await _userManager.FindByIdAsync(ticket.AssignedToUserId);
        }

        if (!string.IsNullOrEmpty(ticket.CreatedByUserId))
        {
            createdByUser = await _userManager.FindByIdAsync(ticket.CreatedByUserId);
        }

        // ← CAMBIO: buscar FullName en tabla Employees por UserId
        string? assignedFullName = null;
        if (!string.IsNullOrEmpty(ticket.AssignedToUserId))
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == ticket.AssignedToUserId);
            assignedFullName = employee?.FullName ?? assignedUser?.UserName;
        }

        // ← NUEVO: Obtener ClientId y ClientName desde Project, ClientId directo, o desde CreatedByUserId
        int? clientId = ticket.Project?.Client?.Id ?? ticket.ClientId;
        string? clientName = ticket.Project?.Client?.CompanyName;

        // Si no hay nombre de cliente (o no hay proyecto), buscar cliente por ClientId directo o CreatedByUserId
        if (string.IsNullOrEmpty(clientName))
        {
            if (ticket.ClientId.HasValue)
            {
                var clientById = await _context.Clients.FindAsync(ticket.ClientId.Value);
                if (clientById != null)
                {
                    clientId = clientById.Id;
                    clientName = clientById.CompanyName;
                }
            }
            else if (!string.IsNullOrEmpty(ticket.CreatedByUserId))
            {
                var clientByUser = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == ticket.CreatedByUserId);
                if (clientByUser != null)
                {
                    clientId = clientByUser.Id;
                    clientName = clientByUser.CompanyName;
                }
            }
        }

        var dto = new TicketDetailDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            ServiceId = ticket.ServiceId ?? 0,
            ServiceName = string.Empty,
            ProjectId = ticket.ProjectId ?? 0,
            ProjectName = ticket.Project?.Name ?? "Sin asignar",
            ClientName = clientName ?? "Sin asignar",
            ClientId = clientId,
            Status = ticket.Status,
            Priority = ticket.Priority,
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedToUserName = assignedFullName,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = createdByUser?.UserName ?? string.Empty,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ClosedAt = ticket.ClosedAt,
            EstimatedHours = ticket.EstimatedHours,
            ActualHours = ticket.ActualHours,
            HourlyRate = 0,

            Comments = ticket.Comments?.Select(c => new TicketCommentDto
            {
                Id = c.Id,
                Comment = c.Comment,
                IsInternal = c.IsInternal,
                UserId = c.UserId,
                UserName = _userManager.FindByIdAsync(c.UserId).Result?.UserName ?? string.Empty,
                CreatedAt = c.CreatedAt
            }).ToList() ?? new List<TicketCommentDto>(),

            Attachments = ticket.Attachments?.Select(a => new TicketAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                UploadedAt = a.UploadedAt
            }).ToList() ?? new List<TicketAttachmentDto>(),

            History = ticket.History?.Select(h => new TicketHistoryDto
            {
                Id = h.Id,
                Action = h.Action,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                UserName = _userManager.FindByIdAsync(h.UserId).Result?.UserName ?? string.Empty,
                ChangedAt = h.ChangedAt
            }).ToList() ?? new List<TicketHistoryDto>(),

            Activities = ticket.Activities?.Select(a => new TicketActivityDto
            {
                Id = a.Id,
                TicketId = a.TicketId,
                Description = a.Description,
                HoursSpent = a.HoursSpent,
                IsCompleted = a.IsCompleted,
                CreatedAt = a.CreatedAt,
                CompletedAt = a.CompletedAt,
                CreatedByUserId = a.CreatedByUserId,
                CreatedByUserName = _userManager.FindByIdAsync(a.CreatedByUserId).Result?.UserName ?? string.Empty
            }).ToList() ?? new List<TicketActivityDto>()
        };

        return dto;
    }
}