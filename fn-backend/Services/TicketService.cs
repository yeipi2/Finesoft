using fs_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public TicketService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IEnumerable<TicketDetailDto>> GetTicketsAsync(string? status = null, string? priority = null,
        int? serviceId = null, string? userId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p.Client)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Include(t => t.Activities)
            .AsQueryable();

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
            query = query.Where(t => t.AssignedToUserId == userId);
        }

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        var ticketDtos = new List<TicketDetailDto>();

        foreach (var ticket in tickets)
        {
            ticketDtos.Add(await MapToDetailDto(ticket));
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
        // Validar que el proyecto existe
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == ticketDto.ProjectId);
        if (!projectExists)
        {
            return ServiceResult<TicketDetailDto>.Failure("El proyecto especificado no existe");
        }

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
            ProjectId = ticketDto.ProjectId,
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
            NewValue = "Ticket creado",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        await _context.Entry(ticket)
            .Reference(t => t.Project)
            .Query()
            .Include(p => p.Client)
            .LoadAsync();

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
        }

        ticket.Title = ticketDto.Title;
        ticket.Description = ticketDto.Description;
        ticket.ProjectId = ticketDto.ProjectId;
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

    public async Task<TicketStatsDto> GetTicketStatsAsync(string? userId = null)
    {
        var query = _context.Tickets.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.AssignedToUserId == userId);
        }

        var tickets = await query.ToListAsync();

        return new TicketStatsDto
        {
            Open = tickets.Count(t => t.Status == "Abierto"),
            InProgress = tickets.Count(t => t.Status == "En Progreso"),
            InReview = tickets.Count(t => t.Status == "En Revisión"),
            Closed = tickets.Count(t => t.Status == "Cerrado"),
            Total = tickets.Count,
            TotalEstimatedHours = tickets.Sum(t => t.EstimatedHours),
            TotalActualHours = tickets.Sum(t => t.ActualHours)
        };
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

        _context.Set<TicketActivity>().Update(activity);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "ActivityUpdated",
            NewValue = $"Actividad actualizada: {activityDto.Description}",
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

        var dto = new TicketDetailDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            ServiceId = ticket.ServiceId ?? 0,
            ServiceName = string.Empty,
            ProjectId = ticket.ProjectId,
            ProjectName = ticket.Project?.Name ?? string.Empty,
            ClientName = ticket.Project?.Client?.CompanyName ?? string.Empty,
            Status = ticket.Status,
            Priority = ticket.Priority,
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedToUserName = assignedUser?.UserName,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = createdByUser?.UserName ?? string.Empty,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ClosedAt = ticket.ClosedAt,
            EstimatedHours = ticket.EstimatedHours,
            ActualHours = ticket.ActualHours,
            HourlyRate = 0,

            // ⭐ MAPEAR LOS COMENTARIOS
            Comments = ticket.Comments?.Select(c => new TicketCommentDto
            {
                Id = c.Id,
                Comment = c.Comment,
                IsInternal = c.IsInternal,
                UserId = c.UserId,
                UserName = _userManager.FindByIdAsync(c.UserId).Result?.UserName ?? string.Empty,
                CreatedAt = c.CreatedAt
            }).ToList() ?? new List<TicketCommentDto>(),

            // ⭐ MAPEAR LOS ATTACHMENTS
            Attachments = ticket.Attachments?.Select(a => new TicketAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
               
                UploadedAt = a.UploadedAt
            }).ToList() ?? new List<TicketAttachmentDto>(),

            // ⭐ MAPEAR EL HISTORIAL
            History = ticket.History?.Select(h => new TicketHistoryDto
            {
                Id = h.Id,
                Action = h.Action,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                UserName = _userManager.FindByIdAsync(h.UserId).Result?.UserName ?? string.Empty,
                ChangedAt = h.ChangedAt
            }).ToList() ?? new List<TicketHistoryDto>(),

            // ⭐⭐⭐ AGREGAR ESTE MAPEO DE ACTIVIDADES ⭐⭐⭐
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